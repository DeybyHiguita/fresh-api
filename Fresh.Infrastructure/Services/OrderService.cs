using Fresh.Core.DTOs.Invoice;
using Fresh.Core.DTOs.Order;
using Fresh.Core.Entities;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fresh.Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly FreshDbContext _context;
    private readonly IInvoiceService _invoiceService;
    private readonly ICustomerCreditService _customerCreditService;

    public OrderService(FreshDbContext context, IInvoiceService invoiceService, ICustomerCreditService customerCreditService)
    {
        _context = context;
        _invoiceService = invoiceService;
        _customerCreditService = customerCreditService;
    }

    public async Task<IEnumerable<OrderResponse>> GetAllAsync(int storeId = 0)
    {
        var query = _context.Orders
            .Include(o => o.User)
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
            .AsQueryable();

        if (storeId > 0)
            query = query.Where(o => o.StoreId == storeId);

        var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
        return orders.Select(MapToResponse);
    }

    public async Task<OrderResponse?> GetByIdAsync(int id)
    {
        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
            .FirstOrDefaultAsync(o => o.Id == id);

        return order == null ? null : MapToResponse(order);
    }

    public async Task<OrderResponse> CreateAsync(OrderRequest request, int storeId = 0)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        // Validar que el usuario (cajero) exista
        var userExists = await _context.Users.AnyAsync(u => u.Id == request.UserId);
        if (!userExists) throw new KeyNotFoundException($"El usuario con ID {request.UserId} no existe.");

        // Validar que los productos existan
        var menuItemIds = request.Items.Select(i => i.MenuItemId).ToList();
        var validItemsCount = await _context.MenuItems.CountAsync(m => menuItemIds.Contains(m.Id));
        if (validItemsCount != menuItemIds.Distinct().Count())
            throw new InvalidOperationException("Uno o más productos no existen en el menú.");

        // Calcular subtotales
        decimal subtotal = request.Items.Sum(i => i.Quantity * i.UnitPrice);
        decimal surcharge = request.OrderType == "Delivery" ? request.DeliverySurcharge : 0m;
        decimal total = subtotal + surcharge - request.Discount;
        if (total < 0) throw new InvalidOperationException("El descuento no puede ser mayor al subtotal.");

        // Validar cliente registrado y crédito si aplica
        CustomerCredit? customerCredit = null;
        if (request.CustomerId.HasValue)
        {
            var customer = await _context.Customers
                .Include(c => c.CreditInfo)
                .FirstOrDefaultAsync(c => c.Id == request.CustomerId.Value && c.IsActive);

            if (customer == null)
                throw new KeyNotFoundException($"El cliente con ID {request.CustomerId.Value} no existe o está inactivo.");

            // Autocompletar nombre si viene vacío
            if (string.IsNullOrWhiteSpace(request.CustomerName))
                request.CustomerName = $"{customer.FirstName} {customer.LastName}";
            if (string.IsNullOrWhiteSpace(request.CustomerPhone) && !string.IsNullOrWhiteSpace(customer.Phone))
                request.CustomerPhone = customer.Phone;

            if (request.PaymentMethod == "Crédito")
            {
                customerCredit = customer.CreditInfo;
                if (customerCredit == null)
                    throw new InvalidOperationException("El cliente no tiene cuenta de crédito configurada.");

                decimal available = customerCredit.CreditLimit - customerCredit.CurrentBalance;
                if (available < total)
                    throw new InvalidOperationException(
                        $"Cupo insuficiente. Disponible: ${available:F2}, Total de la orden: ${total:F2}.");
            }
        }
        else if (request.PaymentMethod == "Crédito")
        {
            throw new InvalidOperationException("Debes seleccionar un cliente registrado para usar pago a crédito.");
        }

        var order = new Order
        {
            StoreId = storeId,
            UserId = request.UserId,
            CustomerId = request.CustomerId,
            CustomerName = request.CustomerName,
            CustomerPhone = request.CustomerPhone,
            Subtotal = subtotal,
            Discount = request.Discount,
            Total = total,
            OrderType = request.OrderType,
            PaymentMethod = request.PaymentMethod,
            Status = "Pendiente",
            Notes = request.Notes,
            DeliveryPlatform = request.OrderType == "Delivery" ? request.DeliveryPlatform : null,
            PlatformPayment = request.OrderType == "Delivery" ? request.PlatformPayment : null,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            OrderItems = request.Items.Select(i => new OrderItem
            {
                MenuItemId = i.MenuItemId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Subtotal = i.Quantity * i.UnitPrice,
                ItemNotes = i.ItemNotes,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            }).ToList()
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        if (request.PaymentMethod == "Crédito" && order.CustomerId.HasValue)
        {
            await _customerCreditService.RegisterPurchaseAsync(order.CustomerId.Value, order.Total, order.Id);
        }

        await transaction.CommitAsync();

        return await GetByIdAsync(order.Id) ?? throw new Exception("Error al recuperar la orden creada.");
    }

    public async Task<OrderResponse?> UpdateStatusAsync(int id, string newStatus, string? notes = null)
    {
        var order = await _context.Orders
            .Include(o => o.Customer)
                .ThenInclude(c => c!.CreditInfo)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) throw new KeyNotFoundException($"Orden {id} no encontrada.");

        // Validar transiciones permitidas
        var currentStatus = order.Status;

        if (currentStatus == "Cancelado")
            throw new InvalidOperationException("La orden ya está cancelada y no puede cambiar de estado.");

        if (currentStatus == "Entregado" && newStatus != "Cancelado")
            throw new InvalidOperationException("La orden ya fue entregada y no puede cambiar de estado.");

        if (currentStatus == "Pendiente" && newStatus != "Cancelado" && newStatus != "Entregado")
            throw new InvalidOperationException("Una orden pendiente solo puede pasar a 'Entregado' o 'Cancelado'.");

        order.Status = newStatus;
        order.UpdatedAt = DateTimeOffset.UtcNow;
        if (newStatus == "Cancelado" && !string.IsNullOrWhiteSpace(notes))
            order.Notes = notes;
        _context.Orders.Update(order);

        // Descontar crédito cuando la orden es entregada, solo si aún no se cargó al crearla
        if (newStatus == "Entregado" && order.CustomerId.HasValue && order.PaymentMethod == "Crédito")
        {
            var alreadyCharged = await _context.CreditTransactions
                .AnyAsync(t => t.OrderId == order.Id && t.Type == "Cargo");

            if (!alreadyCharged)
            {
                await _customerCreditService.RegisterPurchaseAsync(order.CustomerId.Value, order.Total, order.Id);
            }
        }

        await _context.SaveChangesAsync();

        // Generar factura automáticamente al entregar, si aún no existe.
        if (newStatus == "Entregado")
        {
            var alreadyInvoiced = await _context.Invoices.AnyAsync(i => i.OrderId == order.Id);
            if (!alreadyInvoiced)
            {
                await _invoiceService.CreateAsync(new InvoiceRequest
                {
                    OrderId       = order.Id,
                    CustomerName  = order.CustomerName,
                    PaymentMethod = order.PaymentMethod,
                    TaxAmount     = 0m,
                    CashTendered  = order.Total,
                });
            }
        }

        return await GetByIdAsync(id);
    }

    public async Task<OrderResponse?> UpdatePaymentMethodAsync(int id, string paymentMethod, int? customerId = null)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        var order = await _context.Orders
            .Include(o => o.Customer)
                .ThenInclude(c => c!.CreditInfo)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) throw new KeyNotFoundException($"Orden {id} no encontrada.");

        if (order.Status == "Cancelado")
            throw new InvalidOperationException("No se puede cambiar el medio de pago de una orden cancelada.");

        if (string.IsNullOrWhiteSpace(paymentMethod))
            throw new InvalidOperationException("El medio de pago no puede estar vacío.");

        if (paymentMethod == "Crédito")
        {
            var targetCustomerId = customerId ?? order.CustomerId
                ?? throw new InvalidOperationException("Debes seleccionar un cliente para usar pago a crédito.");

            var customer = await _context.Customers
                .Include(c => c.CreditInfo)
                .FirstOrDefaultAsync(c => c.Id == targetCustomerId && c.IsActive);

            if (customer == null)
                throw new KeyNotFoundException($"El cliente con ID {targetCustomerId} no existe o está inactivo.");

            if (customer.CreditInfo == null)
                throw new InvalidOperationException("El cliente no tiene cuenta de crédito configurada.");

            order.CustomerId = customer.Id;
            order.CustomerName = string.IsNullOrWhiteSpace(order.CustomerName)
                ? $"{customer.FirstName} {customer.LastName}"
                : order.CustomerName;
            order.CustomerPhone = string.IsNullOrWhiteSpace(order.CustomerPhone)
                ? customer.Phone
                : order.CustomerPhone;

            var alreadyCharged = await _context.CreditTransactions
                .AnyAsync(t => t.OrderId == order.Id && t.Type == "Cargo");

            if (!alreadyCharged)
            {
                await _customerCreditService.RegisterPurchaseAsync(customer.Id, order.Total, order.Id);
            }
        }

        order.PaymentMethod = paymentMethod;
        order.UpdatedAt = DateTimeOffset.UtcNow;
        _context.Orders.Update(order);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return await GetByIdAsync(id);
    }

    public async Task<OrderResponse?> UpdateItemsAsync(int id, List<OrderItemRequest> items)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        var order = await _context.Orders
            .Include(o => o.Customer)
                .ThenInclude(c => c!.CreditInfo)
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) throw new KeyNotFoundException($"Orden {id} no encontrada.");

        if (order.Status == "Cancelado")
            throw new InvalidOperationException("No se pueden modificar los productos de una orden cancelada.");

        // Validar que los productos existan
        var menuItemIds = items.Select(i => i.MenuItemId).ToList();
        var validCount = await _context.MenuItems.CountAsync(m => menuItemIds.Contains(m.Id));
        if (validCount != menuItemIds.Distinct().Count())
            throw new InvalidOperationException("Uno o más productos no existen en el menú.");

        // Eliminar items existentes y agregar los nuevos
        _context.OrderItems.RemoveRange(order.OrderItems);

        var newItems = items.Select(i => new OrderItem
        {
            OrderId    = order.Id,
            MenuItemId = i.MenuItemId,
            Quantity   = i.Quantity,
            UnitPrice  = i.UnitPrice,
            Subtotal   = i.Quantity * i.UnitPrice,
            ItemNotes  = i.ItemNotes,
            CreatedAt  = DateTimeOffset.UtcNow,
            UpdatedAt  = DateTimeOffset.UtcNow
        }).ToList();

        await _context.OrderItems.AddRangeAsync(newItems);

        // Recalcular totales
        decimal oldTotal = order.Total;
        decimal subtotal = newItems.Sum(i => i.Subtotal);
        decimal oldSurcharge = order.Total - order.Subtotal + order.Discount;
        if (oldSurcharge < 0) oldSurcharge = 0;
        decimal newTotal = subtotal + oldSurcharge - order.Discount;
        if (newTotal < 0) newTotal = 0;

        order.Subtotal   = subtotal;
        order.Total      = newTotal;
        order.UpdatedAt  = DateTimeOffset.UtcNow;
        _context.Orders.Update(order);

        var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.OrderId == order.Id);
        if (invoice != null)
        {
            invoice.Subtotal = newTotal;
            invoice.TotalAmount = newTotal + invoice.TaxAmount;
            invoice.DiscountAmount = order.Discount;
            invoice.CustomerName = order.CustomerName;
            invoice.UpdatedAt = DateTimeOffset.UtcNow;

            if (invoice.PaymentMethod.Equals("Efectivo", StringComparison.OrdinalIgnoreCase))
            {
                invoice.ChangeAmount = Math.Max(0m, invoice.CashTendered - invoice.TotalAmount);
            }

            _context.Invoices.Update(invoice);
        }

        if (order.PaymentMethod == "Crédito" && order.CustomerId.HasValue)
        {
            var customerCredit = order.Customer?.CreditInfo
                ?? throw new InvalidOperationException("El cliente no tiene cuenta de crédito configurada.");

            var creditTransactionExists = await _context.CreditTransactions
                .AnyAsync(t => t.OrderId == order.Id && t.Type == "Cargo");

            if (creditTransactionExists)
            {
                decimal difference = newTotal - oldTotal;
                if (difference != 0)
                {
                    decimal balanceBefore = customerCredit.CurrentBalance;
                    customerCredit.CurrentBalance += difference;
                    customerCredit.Status = customerCredit.CurrentBalance >= customerCredit.CreditLimit
                        ? "Límite alcanzado"
                        : customerCredit.CurrentBalance > 0 ? "Con deuda" : "Al día";
                    customerCredit.UpdatedAt = DateTimeOffset.UtcNow;
                    _context.CustomerCredits.Update(customerCredit);

                    _context.CreditTransactions.Add(new CreditTransaction
                    {
                        CustomerCreditId = customerCredit.Id,
                        OrderId = order.Id,
                        Type = "Ajuste",
                        Amount = difference,
                        BalanceBefore = balanceBefore,
                        BalanceAfter = customerCredit.CurrentBalance,
                        Description = $"Ajuste por edición de la orden #{order.Id}",
                        CreatedAt = DateTimeOffset.UtcNow
                    });
                }
            }
            else if (order.Status == "Entregado")
            {
                await _customerCreditService.RegisterPurchaseAsync(order.CustomerId.Value, newTotal, order.Id);
            }
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return await GetByIdAsync(id);
    }

    /// <inheritdoc />
    public async Task<List<OrderMatchDto>> FindPendingTransferOrdersAsync(decimal amount)
    {
        // Buscar órdenes pendientes con pago transferencia y monto igual (±$100 de tolerancia)
        var tolerance = 100m;
        var orders = await _context.Orders
            .Where(o => o.Status == "Pendiente"
                     && o.PaymentMethod == "Transferencia"
                     && o.Total >= amount - tolerance
                     && o.Total <= amount + tolerance)
            .OrderByDescending(o => o.CreatedAt)
            .Take(10)
            .Select(o => new OrderMatchDto(
                o.Id,
                o.Total,
                o.CustomerName,
                o.CustomerPhone,
                o.PaymentMethod,
                o.Status,
                o.CreatedAt
            ))
            .ToListAsync();

        return orders;
    }

    private static OrderResponse MapToResponse(Order o) => new()
    {
        Id = o.Id,
        UserId = o.UserId,
        UserName = o.User?.Name ?? "Desconocido",
        CustomerId = o.CustomerId,
        CustomerName = o.CustomerName,
        CustomerPhone = o.CustomerPhone,
        Subtotal = o.Subtotal,
        Discount = o.Discount,
        Total = o.Total,
        OrderType = o.OrderType,
        PaymentMethod = o.PaymentMethod,
        Status = o.Status,
        Notes = o.Notes,
        DeliveryPlatform = o.DeliveryPlatform,
        PlatformPayment = o.PlatformPayment,
        CreatedAt = o.CreatedAt,
        UpdatedAt = o.UpdatedAt,
        Items = o.OrderItems.Select(oi => new OrderItemResponse
        {
            Id = oi.Id,
            MenuItemId = oi.MenuItemId,
            MenuItemName = oi.MenuItem?.Name ?? "Desconocido",
            Quantity = oi.Quantity,
            UnitPrice = oi.UnitPrice,
            Subtotal = oi.Subtotal,
            ItemNotes = oi.ItemNotes
        }).ToList()
    };
}

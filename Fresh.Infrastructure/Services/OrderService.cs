using Fresh.Core.DTOs.Order;
using Fresh.Core.Entities;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fresh.Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly FreshDbContext _context;

    public OrderService(FreshDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<OrderResponse>> GetAllAsync()
    {
        var orders = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

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

    public async Task<OrderResponse> CreateAsync(OrderRequest request)
    {
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

        if (currentStatus == "Entregado")
            throw new InvalidOperationException("La orden ya fue entregada y no puede cambiar de estado.");

        if (currentStatus == "Pendiente" && newStatus != "Cancelado" && newStatus != "Entregado")
            throw new InvalidOperationException("Una orden pendiente solo puede pasar a 'Entregado' o 'Cancelado'.");

        order.Status = newStatus;
        order.UpdatedAt = DateTimeOffset.UtcNow;
        if (newStatus == "Cancelado" && !string.IsNullOrWhiteSpace(notes))
            order.Notes = notes;
        _context.Orders.Update(order);

        // Descontar crédito cuando la orden es entregada
        if (newStatus == "Entregado" && order.CustomerId.HasValue && order.PaymentMethod == "Crédito")
        {
            var customerCredit = order.Customer?.CreditInfo;
            if (customerCredit != null)
            {
                decimal balanceBefore = customerCredit.CurrentBalance;
                customerCredit.CurrentBalance += order.Total;
                customerCredit.Status = customerCredit.CurrentBalance >= customerCredit.CreditLimit
                    ? "Límite alcanzado"
                    : customerCredit.CurrentBalance > 0 ? "Con deuda" : "Al día";
                customerCredit.UpdatedAt = DateTimeOffset.UtcNow;
                _context.CustomerCredits.Update(customerCredit);

                await _context.SaveChangesAsync();

                _context.CreditTransactions.Add(new CreditTransaction
                {
                    CustomerCreditId = customerCredit.Id,
                    OrderId = order.Id,
                    Type = "Cargo",
                    Amount = order.Total,
                    BalanceBefore = balanceBefore,
                    BalanceAfter = customerCredit.CurrentBalance,
                    Description = $"Compra - Orden #{order.Id}",
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
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

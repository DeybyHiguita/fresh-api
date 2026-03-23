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
        decimal total = subtotal - request.Discount;
        if (total < 0) throw new InvalidOperationException("El descuento no puede ser mayor al subtotal.");

        // Validar cliente registrado y crédito si aplica
        CustomerCredit? customerCredit = null;
        decimal creditBalanceBefore = 0m;
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

                creditBalanceBefore = customerCredit.CurrentBalance;
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

        // Descontar cupo INMEDIATAMENTE al registrar la orden
        if (customerCredit != null)
        {
            customerCredit.CurrentBalance += total;
            customerCredit.Status = customerCredit.CurrentBalance >= customerCredit.CreditLimit
                ? "Límite alcanzado"
                : customerCredit.CurrentBalance > 0 ? "Con deuda" : "Al día";
            customerCredit.UpdatedAt = DateTimeOffset.UtcNow;
            _context.CustomerCredits.Update(customerCredit);
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Registrar transacción de crédito con el id de la orden ya generado
        if (customerCredit != null)
        {
            _context.CreditTransactions.Add(new CreditTransaction
            {
                CustomerCreditId = customerCredit.Id,
                OrderId = order.Id,
                Type = "Cargo",
                Amount = total,
                BalanceBefore = creditBalanceBefore,
                BalanceAfter = customerCredit.CurrentBalance,
                Description = $"Compra - Orden #{order.Id}",
                CreatedAt = DateTimeOffset.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        return await GetByIdAsync(order.Id) ?? throw new Exception("Error al recuperar la orden creada.");
    }

    public async Task<OrderResponse?> UpdateStatusAsync(int id, string newStatus)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) throw new KeyNotFoundException($"Orden {id} no encontrada.");

        order.Status = newStatus;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        _context.Orders.Update(order);
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

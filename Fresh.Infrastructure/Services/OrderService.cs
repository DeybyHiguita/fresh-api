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

        var order = new Order
        {
            UserId = request.UserId,
            CustomerName = request.CustomerName,
            CustomerPhone = request.CustomerPhone,
            Subtotal = subtotal,
            Discount = request.Discount,
            Total = total,
            OrderType = request.OrderType,
            PaymentMethod = request.PaymentMethod,
            Status = "Pendiente", // Por defecto
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

        // Recargar la orden para obtener nombres de Usuario y MenuItems y mapear correctamente
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

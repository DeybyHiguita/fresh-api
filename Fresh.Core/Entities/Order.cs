namespace Fresh.Core.Entities;

public class Order
{
    public int Id { get; set; }
    public int StoreId { get; set; }
    public int UserId { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public decimal Subtotal { get; set; } = 0m;
    public decimal Discount { get; set; } = 0m;
    public decimal Total { get; set; } = 0m;
    public string OrderType { get; set; } = "Local";
    public string PaymentMethod { get; set; } = "Efectivo";
    public string Status { get; set; } = "Pendiente";
    public string? Notes { get; set; }
    public string? DeliveryPlatform { get; set; }
    public decimal? PlatformPayment { get; set; }
    public bool IsCreditPaid { get; set; } = false;
    public decimal? AmountPaid { get; set; } = null;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Relaciones de navegación
    public Store? Store { get; set; }
    public User? User { get; set; }
    public Customer? Customer { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = [];
}

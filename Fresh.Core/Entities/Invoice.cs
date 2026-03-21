namespace Fresh.Core.Entities;

public class Invoice
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string? CustomerDocument { get; set; }
    public string? CustomerName { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; } = 0m;
    public decimal DiscountAmount { get; set; } = 0m;
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal CashTendered { get; set; } = 0m;
    public decimal ChangeAmount { get; set; } = 0m;
    
    // Obligatorios por convención
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Relación de navegación
    public Order? Order { get; set; }
}
namespace Fresh.Core.DTOs.Invoice;

public class InvoiceResponse
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string? CustomerDocument { get; set; }
    public string? CustomerName { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal CashTendered { get; set; }
    public decimal ChangeAmount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
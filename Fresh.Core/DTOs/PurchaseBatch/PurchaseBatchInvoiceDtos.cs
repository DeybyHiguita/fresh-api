namespace Fresh.Core.DTOs.PurchaseBatch;

public class PurchaseBatchInvoiceRequest
{
    public string? Supplier { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateOnly? InvoiceDate { get; set; }
    public decimal Total { get; set; }
    public string? ImageUrl { get; set; }
    public string? FileName { get; set; }
}

public class PurchaseBatchInvoiceResponse
{
    public int Id { get; set; }
    public int BatchId { get; set; }
    public string? Supplier { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateOnly? InvoiceDate { get; set; }
    public decimal Total { get; set; }
    public string? ImageUrl { get; set; }
    public string? FileName { get; set; }
    public DateTime CreatedAt { get; set; }
}

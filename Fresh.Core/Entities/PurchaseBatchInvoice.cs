namespace Fresh.Core.Entities;

/// <summary>
/// Factura de compra escaneada y asociada a un lote de compra.
/// Guarda los metadatos detectados por la IA y el enlace a la imagen (Google Drive).
/// </summary>
public class PurchaseBatchInvoice
{
    public int Id { get; set; }
    public int BatchId { get; set; }
    public string? Supplier { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateOnly? InvoiceDate { get; set; }
    public decimal Total { get; set; }
    public string? ImageUrl { get; set; }
    public string? FileName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Relación
    public PurchaseBatch Batch { get; set; } = null!;
}

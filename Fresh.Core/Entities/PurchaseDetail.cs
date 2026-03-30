namespace Fresh.Core.Entities;

public class PurchaseDetail
{
    public int Id { get; set; }
    public int BatchId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal TotalValue { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Relaciones
    public PurchaseBatch Batch { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

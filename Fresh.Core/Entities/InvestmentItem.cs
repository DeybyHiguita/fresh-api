namespace Fresh.Core.Entities;

public class InvestmentItem
{
    public int Id { get; set; }
    public int InvestmentId { get; set; }
    
    public string ItemType { get; set; } = string.Empty; // equipment, purchase_batch, product, other
    
    public int? EquipmentId { get; set; }
    public int? PurchaseBatchId { get; set; }
    public int? ProductId { get; set; }
    
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navegación
    public Investment? Investment { get; set; }
    public Equipment? Equipment { get; set; }
    public PurchaseBatch? PurchaseBatch { get; set; }
    public Product? Product { get; set; }
}

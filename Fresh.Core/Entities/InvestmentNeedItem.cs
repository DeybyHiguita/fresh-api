namespace Fresh.Core.Entities;

public class InvestmentNeedItem
{
    public int Id { get; set; }
    public int NeedId { get; set; }
    public string? ItemType { get; set; }          // "equipment" | "purchase_batch" | "product" | "other"
    public int? EquipmentId { get; set; }
    public int? PurchaseBatchId { get; set; }
    public int? ProductId { get; set; }
    public string? Description { get; set; }

    // Referencia para estimar el costo del ítem (sobre todo productos)
    public decimal? Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? EstimatedCost { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    // Navigation
    public InvestmentNeed Need { get; set; } = null!;
    public Equipment? Equipment { get; set; }
    public PurchaseBatch? PurchaseBatch { get; set; }
    public Product? Product { get; set; }
}

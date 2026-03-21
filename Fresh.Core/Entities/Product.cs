namespace Fresh.Core.Entities;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string UnitMeasure { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Relaciones
    public ICollection<PurchaseDetail> PurchaseDetails { get; set; } = [];
    public ICollection<IngredientProduct> IngredientProducts { get; set; } = [];
}

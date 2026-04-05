namespace Fresh.Core.Entities;

public class MenuItemVariant
{
    public int Id { get; set; }
    public int MenuItemId { get; set; }
    public string VariantName { get; set; } = string.Empty;
    public decimal SalePrice { get; set; }
    public decimal PreparationCost { get; set; }
    public bool IsAvailable { get; set; } = true;
    public int SortOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Nav
    public MenuItem MenuItem { get; set; } = null!;
}

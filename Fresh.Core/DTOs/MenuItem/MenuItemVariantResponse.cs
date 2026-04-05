namespace Fresh.Core.DTOs.MenuItem;

public class MenuItemVariantResponse
{
    public int Id { get; set; }
    public int MenuItemId { get; set; }
    public string VariantName { get; set; } = string.Empty;
    public decimal SalePrice { get; set; }
    public decimal PreparationCost { get; set; }
    public bool IsAvailable { get; set; }
    public int SortOrder { get; set; }
}

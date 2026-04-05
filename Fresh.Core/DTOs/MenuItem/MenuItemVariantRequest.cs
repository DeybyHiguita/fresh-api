using System.ComponentModel.DataAnnotations;

namespace Fresh.Core.DTOs.MenuItem;

public class MenuItemVariantRequest
{
    [Required]
    [MaxLength(100)]
    public string VariantName { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal SalePrice { get; set; }

    [Range(0, double.MaxValue)]
    public decimal PreparationCost { get; set; }

    public bool IsAvailable { get; set; } = true;

    [Range(0, int.MaxValue)]
    public int SortOrder { get; set; } = 0;
}

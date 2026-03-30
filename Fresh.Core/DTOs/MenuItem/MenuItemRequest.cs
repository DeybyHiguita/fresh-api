using System.ComponentModel.DataAnnotations;

namespace Fresh.Core.DTOs.MenuItem;

public class MenuItemRequest
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal PreparationCost { get; set; }

    [Range(0, double.MaxValue)]
    public decimal SalePrice { get; set; }

    public bool IsAvailable { get; set; } = true;

    [MaxLength(2048)]
    public string? ImgUrl { get; set; }

    [Range(0, int.MaxValue)]
    public int SortOrder { get; set; } = 0;
}

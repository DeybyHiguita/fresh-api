using System.ComponentModel.DataAnnotations;

namespace Fresh.Core.DTOs.Product;

public class ProductRequest
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string UnitMeasure { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal CurrentStock { get; set; } = 0;

    public bool IsActive { get; set; } = true;
}

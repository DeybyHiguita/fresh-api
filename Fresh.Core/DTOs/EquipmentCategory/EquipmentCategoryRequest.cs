using System.ComponentModel.DataAnnotations;

namespace Fresh.Core.DTOs.EquipmentCategory;

public class EquipmentCategoryRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

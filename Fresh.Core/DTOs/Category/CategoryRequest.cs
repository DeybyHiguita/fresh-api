using System.ComponentModel.DataAnnotations;

namespace Fresh.Core.DTOs.Category;

public class CategoryRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }
}

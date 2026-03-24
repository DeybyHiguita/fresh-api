using System.ComponentModel.DataAnnotations;

namespace Fresh.Core.DTOs.AppPage;

public class AppPageRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Route { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Icon { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
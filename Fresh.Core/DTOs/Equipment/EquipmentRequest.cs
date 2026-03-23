using System.ComponentModel.DataAnnotations;

namespace Fresh.Core.DTOs.Equipment;

public class EquipmentRequest
{
    [Required]
    public int CategoryId { get; set; }

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Brand { get; set; }

    [MaxLength(100)]
    public string? Model { get; set; }

    [MaxLength(100)]
    public string? SerialNumber { get; set; }

    public DateOnly? PurchaseDate { get; set; }

    [Range(0, double.MaxValue)]
    public decimal PurchasePrice { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Activo";

    [MaxLength(100)]
    public string? Location { get; set; }

    public string? Notes { get; set; }
}

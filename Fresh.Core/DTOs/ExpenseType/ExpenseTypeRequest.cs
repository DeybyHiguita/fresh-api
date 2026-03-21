using System.ComponentModel.DataAnnotations;

namespace Fresh.Core.DTOs.ExpenseType;

public class ExpenseTypeRequest
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Range(0, double.MaxValue)]
    public decimal ExpectedAmount { get; set; }

    [Required]
    [MaxLength(50)]
    public string Frequency { get; set; } = "Mensual";

    public bool IsActive { get; set; } = true;
}
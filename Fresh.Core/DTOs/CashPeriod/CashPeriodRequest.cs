using System.ComponentModel.DataAnnotations;

namespace Fresh.Core.DTOs.CashPeriod;

public class CashPeriodRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateOnly StartDate { get; set; }

    [Required]
    public DateOnly EndDate { get; set; }
}

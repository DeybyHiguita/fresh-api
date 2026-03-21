using System.ComponentModel.DataAnnotations;

namespace Fresh.Core.DTOs.PurchaseBatch;

public class PurchaseBatchRequest
{
    [Required]
    [MaxLength(100)]
    public string BatchName { get; set; } = string.Empty;

    [Required]
    public DateOnly StartDate { get; set; }

    [Required]
    public DateOnly EndDate { get; set; }
}

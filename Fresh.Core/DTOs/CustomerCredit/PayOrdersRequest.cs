using System.ComponentModel.DataAnnotations;

namespace Fresh.Core.DTOs.CustomerCredit;

public class PayOrdersRequest
{
    [Required]
    public List<int> OrderIds { get; set; } = [];
    public string? PaymentMethod { get; set; }
    public string? Notes { get; set; }
}

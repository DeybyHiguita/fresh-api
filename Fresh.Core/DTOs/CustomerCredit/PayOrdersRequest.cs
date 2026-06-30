using System.ComponentModel.DataAnnotations;

namespace Fresh.Core.DTOs.CustomerCredit;

public class OrderPayItem
{
    public int OrderId { get; set; }
    public decimal AmountPaid { get; set; }
}

public class PayOrdersRequest
{
    [Required]
    public List<OrderPayItem> Orders { get; set; } = [];
    public string? PaymentMethod { get; set; }
    public string? Notes { get; set; }
}

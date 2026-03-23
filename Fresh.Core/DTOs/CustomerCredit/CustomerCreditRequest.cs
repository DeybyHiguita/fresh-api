using System.ComponentModel.DataAnnotations;

namespace Fresh.Core.DTOs.CustomerCredit;

public class CustomerCreditRequest
{
    [Required]
    public int CustomerId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal CreditLimit { get; set; }

    [Required]
    [MaxLength(50)]
    public string PaymentFrequency { get; set; } = "Mensual";
}
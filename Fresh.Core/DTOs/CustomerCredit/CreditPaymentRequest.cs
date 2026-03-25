using System.ComponentModel.DataAnnotations;

namespace Fresh.Core.DTOs.CustomerCredit;

public class CreditPaymentRequest
{
    [Range(0.01, double.MaxValue, ErrorMessage = "El pago debe ser mayor a cero")]
    public decimal Amount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Notes { get; set; }
}
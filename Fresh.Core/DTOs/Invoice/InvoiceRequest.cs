using System.ComponentModel.DataAnnotations;

namespace Fresh.Core.DTOs.Invoice;

public class InvoiceRequest
{
    [Required]
    public int OrderId { get; set; }

    [MaxLength(20)]
    public string? CustomerDocument { get; set; }

    [MaxLength(150)]
    public string? CustomerName { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TaxAmount { get; set; }

    [Required]
    [MaxLength(50)]
    public string PaymentMethod { get; set; } = "Efectivo";

    [Range(0, double.MaxValue)]
    public decimal CashTendered { get; set; }
}
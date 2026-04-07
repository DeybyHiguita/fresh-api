using System.ComponentModel.DataAnnotations;

namespace Fresh.Core.DTOs.CashRegister;

public class CloseCashRegisterRequest
{
    [Required]
    public int ClosedById { get; set; }

    [Range(0, double.MaxValue)]
    public decimal ReportedCash { get; set; }

    [Range(0, double.MaxValue)]
    public decimal ReportedTransfer { get; set; }

    [Range(0, double.MaxValue)]
    public decimal ReportedCard { get; set; }

    public string? Observations { get; set; }

    [Range(0, double.MaxValue)]
    public decimal AmountToSafe { get; set; } = 0;
}

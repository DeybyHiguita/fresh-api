using System.ComponentModel.DataAnnotations;

namespace Fresh.Core.DTOs.CashRegister;

public class OpenCashRegisterRequest
{
    [Required]
    public int PeriodId { get; set; }

    [Required]
    public int OpenedById { get; set; }

    [Range(0, double.MaxValue)]
    public decimal OpeningBalance { get; set; }
}

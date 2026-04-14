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

    [Range(0, double.MaxValue)]
    public decimal AmountToBankAccount { get; set; } = 0;

    [Range(0, double.MaxValue)]
    public decimal AmountLeftInRegister { get; set; } = 0;

    /// <summary>IDs de los gastos que el usuario confirma incluir en el cuadre.</summary>
    public List<int>? SelectedExpenseIds { get; set; }
}

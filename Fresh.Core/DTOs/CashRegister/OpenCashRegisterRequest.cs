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

    /// <summary>Observación inicial — conteo de efectivo al abrir.</summary>
    public string? OpeningObservations { get; set; }
}

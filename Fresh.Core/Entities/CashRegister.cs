namespace Fresh.Core.Entities;

public class CashRegister
{
    public int Id { get; set; }
    public int PeriodId { get; set; }
    public int OpenedById { get; set; }
    public int? ClosedById { get; set; }

    public DateTimeOffset OpeningTime { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ClosingTime { get; set; }

    public decimal OpeningBalance { get; set; }

    /// <summary>Observación registrada al abrir la caja (conteo inicial de efectivo).</summary>
    public string? OpeningObservations { get; set; }

    // Lo que reporta el cajero al cerrar
    public decimal? ReportedCash { get; set; }
    public decimal? ReportedTransfer { get; set; }
    public decimal? ReportedCard { get; set; }

    // Lo que calcula el sistema (Facturas - Gastos)
    public decimal? SystemCash { get; set; }
    public decimal? SystemTransfer { get; set; }
    public decimal? SystemCard { get; set; }

    /// <summary>Diferencia firmada: reportado_total − esperado_neto. Null si aún no se ha cerrado.</summary>
    public decimal? Difference { get; set; }

    /// <summary>IDs de gastos que el cajero incluyó al cerrar (JSON array). Null = cierre sin registro de selección.</summary>
    public List<int>? SelectedExpenseIds { get; set; }

    public string Status { get; set; } = "Abierta"; // Abierta, Cerrada, Descuadrada
    public string? Observations { get; set; }
    public decimal AmountToSafe { get; set; } = 0;
    public decimal AmountToBankAccount { get; set; } = 0;
    /// <summary>Efectivo físico dejado en el cajón para el siguiente turno.</summary>
    public decimal AmountLeftInRegister { get; set; } = 0;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public CashPeriod? Period { get; set; }
    public User? OpenedBy { get; set; }
    public User? ClosedBy { get; set; }
}

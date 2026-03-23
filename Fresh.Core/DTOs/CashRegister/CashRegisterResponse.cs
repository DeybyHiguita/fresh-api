namespace Fresh.Core.DTOs.CashRegister;

public class CashRegisterResponse
{
    public int Id { get; set; }
    public int PeriodId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public int OpenedById { get; set; }
    public string OpenedByName { get; set; } = string.Empty;
    public int? ClosedById { get; set; }
    public string? ClosedByName { get; set; }
    public DateTimeOffset OpeningTime { get; set; }
    public DateTimeOffset? ClosingTime { get; set; }
    public decimal OpeningBalance { get; set; }

    public decimal? ReportedCash { get; set; }
    public decimal? ReportedTransfer { get; set; }
    public decimal? ReportedCard { get; set; }

    public decimal? SystemCash { get; set; }
    public decimal? SystemTransfer { get; set; }
    public decimal? SystemCard { get; set; }

    // Propiedad calculada útil para el frontend
    public decimal? CashDifference => (ReportedCash ?? 0) - (SystemCash ?? 0);

    public string Status { get; set; } = string.Empty;
    public string? Observations { get; set; }
}

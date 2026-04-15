namespace Fresh.Core.DTOs.CashRegister;

/// <summary>
/// Permite al administrador corregir los valores reportados y observaciones
/// de una caja ya cerrada (o descuadrada).
/// </summary>
public class EditCashRegisterRequest
{
    public decimal ReportedCash { get; set; }
    public decimal ReportedTransfer { get; set; }
    public decimal ReportedCard { get; set; }
    public string? Observations { get; set; }
    /// <summary>Fuerza el estado: "Cerrada" o "Descuadrada".</summary>
    public string Status { get; set; } = "Cerrada";
    /// <summary>IDs de gastos que el admin quiere incluir. Null = no cambiar la selección existente.</summary>
    public List<int>? SelectedExpenseIds { get; set; }
}

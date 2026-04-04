namespace Fresh.Core.DTOs.PurchaseBatch;

/// <summary>
/// Versión liviana de un lote de compra: solo id, nombre, fechas y total.
/// Sin detalles — ideal para selectores y listas compactas.
/// </summary>
public class PurchaseBatchSummary
{
    public int      Id        { get; set; }
    public string   BatchName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate   { get; set; }
    public decimal  Total     { get; set; }
}

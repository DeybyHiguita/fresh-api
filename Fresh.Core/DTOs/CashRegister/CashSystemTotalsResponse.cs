namespace Fresh.Core.DTOs.CashRegister;

public class CashSystemTotalsResponse
{
    public int RegisterId { get; set; }
    public decimal OpeningBalance { get; set; }

    // Ventas del turno por método de pago (efectivo ya incluye saldo inicial)
    public decimal SystemCash { get; set; }
    public decimal SystemTransfer { get; set; }
    public decimal SystemCard { get; set; }
    /// <summary>
    /// Ventas a crédito (fiado/cupo de cliente). No genera dinero físico en el cajón.
    /// Se muestra solo como referencia informativa.
    /// </summary>
    public decimal SystemCredit { get; set; }
    public decimal TotalInvoices { get; set; }
    public int InvoiceCount { get; set; }

    // Gastos registrados durante el turno por método de pago
    public decimal ExpensesCash { get; set; }
    public decimal ExpensesTransfer { get; set; }
    public decimal ExpensesCard { get; set; }
    public decimal TotalExpenses { get; set; }
    public int ExpenseCount { get; set; }

    // Neto esperado por método = ventas − gastos (efectivo también descuenta)
    public decimal NetCash { get; set; }
    public decimal NetTransfer { get; set; }
    public decimal NetCard { get; set; }

    /// <summary>
    /// Dinero físico total esperado = NetCash + NetTransfer.
    /// Tarjeta excluida porque va al datafono, no al cajón.
    /// </summary>
    public decimal NetMovable => NetCash + NetTransfer;
}

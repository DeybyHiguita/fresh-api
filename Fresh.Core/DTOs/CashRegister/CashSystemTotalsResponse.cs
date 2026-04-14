namespace Fresh.Core.DTOs.CashRegister;

public class ExpenseItemDto
{
    public int Id { get; set; }
    public string ExpenseTypeName { get; set; } = "";
    public decimal AmountPaid { get; set; }
    public string PaymentMethod { get; set; } = "";
    public DateOnly PaymentDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? Notes { get; set; }
}

public class CashSystemTotalsResponse
{
    public int RegisterId { get; set; }
    public decimal OpeningBalance { get; set; }

    // Ventas netas del turno por método de pago (SystemCash incluye saldo inicial)
    public decimal SystemCash { get; set; }
    public decimal SystemTransfer { get; set; }
    public decimal SystemCard { get; set; }
    /// <summary>
    /// Ventas a crédito (fiado/cupo). No genera dinero físico en el cajón al momento.
    /// </summary>
    public decimal SystemCredit { get; set; }

    /// <summary>Ventas brutas en efectivo (sin saldo inicial)</summary>
    public decimal SalesCash { get; set; }
    /// <summary>Ventas brutas en transferencia</summary>
    public decimal SalesTransfer { get; set; }

    public decimal TotalInvoices { get; set; }
    public int InvoiceCount { get; set; }

    // Abonos de crédito cobrados durante el turno
    public decimal CreditPaymentsCash { get; set; }
    public decimal CreditPaymentsTransfer { get; set; }
    public decimal CreditPaymentsTotal { get; set; }

    // Gastos registrados durante el turno por método de pago
    public decimal ExpensesCash { get; set; }
    public decimal ExpensesTransfer { get; set; }
    public decimal ExpensesCard { get; set; }
    public decimal TotalExpenses { get; set; }
    public int ExpenseCount { get; set; }

    /// <summary>Lista detallada de gastos del turno para selección en el frontend</summary>
    public List<ExpenseItemDto> Expenses { get; set; } = [];

    // Neto esperado por método = ventas + abonos crédito − gastos
    public decimal NetCash { get; set; }
    public decimal NetTransfer { get; set; }
    public decimal NetCard { get; set; }

    /// <summary>
    /// Dinero físico total esperado = NetCash + NetTransfer.
    /// Tarjeta excluida porque va al datafono.
    /// </summary>
    public decimal NetMovable => NetCash + NetTransfer;
}

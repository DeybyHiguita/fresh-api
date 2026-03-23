namespace Fresh.Core.DTOs.CashRegister;

public class CashSystemTotalsResponse
{
    public int RegisterId { get; set; }
    public decimal SystemCash { get; set; }
    public decimal SystemTransfer { get; set; }
    public decimal SystemCard { get; set; }
    public decimal TotalInvoices { get; set; }
    public int InvoiceCount { get; set; }
    public decimal OpeningBalance { get; set; }
}

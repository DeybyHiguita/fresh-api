namespace Fresh.Core.DTOs.CashPeriod;

public class CashPeriodResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsClosed { get; set; }

    // Totales de gastos del periodo
    public decimal TotalExpenses { get; set; }
    public decimal ExpensesCash { get; set; }
    public decimal ExpensesTransfer { get; set; }
    public decimal ExpensesCard { get; set; }
    public int ExpenseCount { get; set; }
}

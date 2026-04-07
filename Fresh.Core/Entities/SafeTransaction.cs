namespace Fresh.Core.Entities;

public class SafeTransaction
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty; // "Ingreso" | "Gasto"
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public int? CashRegisterId { get; set; }
    public int? CreatedById { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public CashRegister? CashRegister { get; set; }
    public User? CreatedBy { get; set; }
}

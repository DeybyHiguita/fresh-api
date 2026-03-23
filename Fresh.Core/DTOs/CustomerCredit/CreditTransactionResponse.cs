namespace Fresh.Core.DTOs.CustomerCredit;

public class CreditTransactionResponse
{
    public int Id { get; set; }
    public int CustomerCreditId { get; set; }
    public int? OrderId { get; set; }
    public string Type { get; set; } = string.Empty;        // "Cargo" | "Abono"
    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

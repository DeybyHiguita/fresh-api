namespace Fresh.Core.DTOs.CustomerCredit;

public class PaidDebtReportResponse
{
    public int TransactionId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerDocument { get; set; }
    public string? CustomerPhone { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

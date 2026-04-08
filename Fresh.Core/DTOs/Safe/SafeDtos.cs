namespace Fresh.Core.DTOs.Safe;

public class SafeResponse
{
    public int Id { get; set; }
    public decimal Balance { get; set; }
    public string SafeType { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
}

public class SafeTransactionResponse
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public int? CashRegisterId { get; set; }
    public string? CreatedByName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class SafeExpenseRequest
{
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public int CreatedById { get; set; }
    public string SafeType { get; set; } = "caja_fuerte";
}

public class SafeDepositRequest
{
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public int? CashRegisterId { get; set; }
    public int CreatedById { get; set; }
    public string SafeType { get; set; } = "caja_fuerte";
}

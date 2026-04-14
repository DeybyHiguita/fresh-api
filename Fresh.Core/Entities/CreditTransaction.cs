namespace Fresh.Core.Entities;

public class CreditTransaction
{
    public int Id { get; set; }
    public int CustomerCreditId { get; set; }
    public int? OrderId { get; set; }

    /// <summary>"Cargo" when a purchase uses cupo; "Abono" when customer makes a payment.</summary>
    public string Type { get; set; } = "Cargo";

    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? Description { get; set; }

    /// <summary>Payment method used when Type="Abono" (Efectivo, Transferencia, etc.)</summary>
    public string? PaymentMethod { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public CustomerCredit? CustomerCredit { get; set; }
    public Order? Order { get; set; }
}

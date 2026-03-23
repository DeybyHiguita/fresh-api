namespace Fresh.Core.Entities;

public class CustomerCredit
{
    public int Id { get; set; }
    public int CustomerId { get; set; }

    public decimal CreditLimit { get; set; } = 0m;
    public string PaymentFrequency { get; set; } = "Mensual";

    public decimal CurrentBalance { get; set; } = 0m;
    public string Status { get; set; } = "Al día";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Customer? Customer { get; set; }
}
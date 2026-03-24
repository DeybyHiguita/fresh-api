namespace Fresh.Core.DTOs.CustomerCredit;

public class CustomerCreditResponse
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; }
    public string PaymentFrequency { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
    public decimal AvailableCredit => CreditLimit - CurrentBalance;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
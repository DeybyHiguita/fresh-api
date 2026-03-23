namespace Fresh.Core.DTOs.Customer;

public class CustomerResponse
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string DocumentNumber { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? ReferenceName { get; set; }
    public string? ReferencePhone { get; set; }
    public int CreatedById { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    // Datos rápidos del crédito sin hacer otra petición
    public bool HasCreditAccount { get; set; }
    public decimal? CreditLimit { get; set; }
    public decimal? CurrentBalance { get; set; }
    public decimal? AvailableCredit => CreditLimit - CurrentBalance;
    public string? CreditStatus { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
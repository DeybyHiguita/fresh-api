namespace Fresh.Core.Entities;

public class Customer
{
    public int Id { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }

    public string? ReferenceName { get; set; }
    public string? ReferencePhone { get; set; }

    public int CreatedById { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Relaciones
    public User? CreatedBy { get; set; }
    public CustomerCredit? CreditInfo { get; set; }
}
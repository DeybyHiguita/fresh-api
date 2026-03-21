namespace Fresh.Core.Entities;

public class Expense
{
    public int Id { get; set; }
    public int ExpenseTypeId { get; set; }
    public int UserId { get; set; }
    
    public decimal AmountPaid { get; set; }
    public DateOnly PaymentDate { get; set; }
    public string PaymentMethod { get; set; } = "Efectivo";
    public string? Notes { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Relaciones de navegación
    public ExpenseType? ExpenseType { get; set; }
    public User? User { get; set; }
}
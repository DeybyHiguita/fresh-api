namespace Fresh.Core.Entities;

public class ExpenseType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal ExpectedAmount { get; set; }
    public string Frequency { get; set; } = "Mensual";
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Relación
    public ICollection<Expense> Expenses { get; set; } = [];
    
}
namespace Fresh.Core.DTOs.Expense;

public class ExpenseResponse
{
    public int Id { get; set; }
    public int ExpenseTypeId { get; set; }
    public string ExpenseTypeName { get; set; } = string.Empty; // Extraído de la relación
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;       // Extraído de la relación
    public decimal AmountPaid { get; set; }
    public DateOnly PaymentDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
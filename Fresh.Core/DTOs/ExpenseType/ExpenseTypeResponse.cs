namespace Fresh.Core.DTOs.ExpenseType;

public class ExpenseTypeResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal ExpectedAmount { get; set; }
    public string Frequency { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
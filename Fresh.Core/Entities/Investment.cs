namespace Fresh.Core.Entities;

public class Investment
{
    public int Id { get; set; }
    public int InvestorId { get; set; }
    
    public decimal Amount { get; set; }
    public DateOnly InvestmentDate { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "Activo";
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navegación
    public User? Investor { get; set; }
    public ICollection<InvestmentItem> Items { get; set; } = [];
}

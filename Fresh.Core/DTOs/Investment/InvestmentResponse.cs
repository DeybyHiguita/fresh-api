namespace Fresh.Core.DTOs.Investment;

public class InvestmentResponse
{
    public int Id { get; set; }
    public int InvestorId { get; set; }
    public string InvestorName { get; set; } = string.Empty;
    
    public decimal Amount { get; set; }
    public decimal JustifiedAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public double JustifiedPercentage { get; set; }
    
    public DateOnly InvestmentDate { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "Activo";
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    
    public List<InvestmentItemResponse> Items { get; set; } = [];
}

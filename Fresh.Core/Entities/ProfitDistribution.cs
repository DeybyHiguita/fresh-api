namespace Fresh.Core.Entities;

public class ProfitDistribution
{
    public int Id { get; set; }
    public decimal TotalProfit { get; set; }
    public string? Notes { get; set; }
    public int CreatedById { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public User? CreatedBy { get; set; }
    public ICollection<ProfitDistributionShare> Shares { get; set; } = [];
}

namespace Fresh.Core.Entities;

public class ProfitDistributionShare
{
    public int Id { get; set; }
    public int DistributionId { get; set; }
    public int InvestorId { get; set; }
    public string InvestorName { get; set; } = string.Empty;
    public decimal InvestedCapital { get; set; }
    public decimal ParticipationPct { get; set; }
    public decimal ShareAmount { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public ProfitDistribution? Distribution { get; set; }
    public User? Investor { get; set; }
}

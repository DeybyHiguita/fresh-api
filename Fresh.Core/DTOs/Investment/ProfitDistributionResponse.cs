namespace Fresh.Core.DTOs.Investment;

public class ProfitDistributionResponse
{
    public int Id { get; set; }
    public decimal TotalProfit { get; set; }
    public string? Notes { get; set; }
    public int CreatedById { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public List<ProfitDistributionShareResponse> Shares { get; set; } = [];
}

public class ProfitDistributionShareResponse
{
    public int Id { get; set; }
    public int InvestorId { get; set; }
    public string InvestorName { get; set; } = string.Empty;
    public decimal InvestedCapital { get; set; }
    public decimal ParticipationPct { get; set; }
    public decimal ShareAmount { get; set; }
}

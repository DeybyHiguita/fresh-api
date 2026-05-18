namespace Fresh.Core.DTOs.Investment;

public class ProfitDistributionRequest
{
    public decimal TotalProfit { get; set; }
    public string? Notes { get; set; }
    public List<ProfitDistributionShareRequest> Shares { get; set; } = [];
}

public class ProfitDistributionShareRequest
{
    public int InvestorId { get; set; }
    public string InvestorName { get; set; } = string.Empty;
    public decimal InvestedCapital { get; set; }
    public decimal ParticipationPct { get; set; }
    public decimal ShareAmount { get; set; }
}

namespace Fresh.Core.DTOs.EmployeeAffiliation;

public class EmployeeAffiliationRequest
{
    public string AffiliationType { get; set; } = string.Empty; // health, pension, arl, compensation_fund, severance_fund
    public string EntityName { get; set; } = string.Empty;
    public string? EntityCode { get; set; }
    public string Status { get; set; } = "active"; // active, inactive, pending, suspended
    public DateTime? AffiliationDate { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public string? PlanType { get; set; }
    public decimal? CoveragePercentage { get; set; }
    public decimal? ContributionBase { get; set; }
    public string? Notes { get; set; }
}

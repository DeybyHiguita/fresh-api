namespace Fresh.Core.DTOs.EmployeeAffiliation;

public class EmployeeAffiliationResponse
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    
    public string AffiliationType { get; set; } = string.Empty;
    public string AffiliationTypeDisplay { get; set; } = string.Empty;
    
    public string EntityName { get; set; } = string.Empty;
    public string? EntityCode { get; set; }
    
    public string Status { get; set; } = string.Empty;
    public string StatusDisplay { get; set; } = string.Empty;
    
    public DateTime? AffiliationDate { get; set; }
    public DateTime? EffectiveDate { get; set; }
    
    public string? PlanType { get; set; }
    public decimal? CoveragePercentage { get; set; }
    public decimal? ContributionBase { get; set; }
    
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

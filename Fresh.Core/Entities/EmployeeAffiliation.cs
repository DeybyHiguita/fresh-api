namespace Fresh.Core.Entities;

/// <summary>
/// Estado de afiliaciones del empleado
/// (Salud, Pensión, ARL, Caja de Compensación, Cesantías)
/// </summary>
public class EmployeeAffiliation
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    
    // Tipo: health, pension, arl, compensation_fund, severance_fund
    public string AffiliationType { get; set; } = string.Empty;
    
    // Información de la entidad
    public string EntityName { get; set; } = string.Empty;
    public string? EntityCode { get; set; }
    
    // Estado: active, inactive, pending, suspended
    public string Status { get; set; } = "active";
    public DateTime? AffiliationDate { get; set; }
    public DateTime? EffectiveDate { get; set; }
    
    // Detalles adicionales
    public string? PlanType { get; set; }
    public decimal? CoveragePercentage { get; set; }
    public decimal? ContributionBase { get; set; }
    
    public string? Notes { get; set; }
    
    // Auditoría
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navegación
    public Employee? Employee { get; set; }
    
    // Helper para mostrar el tipo en español
    public string AffiliationTypeDisplay => AffiliationType switch
    {
        "health" => "Salud (EPS)",
        "pension" => "Pensión",
        "arl" => "ARL",
        "compensation_fund" => "Caja de Compensación",
        "severance_fund" => "Cesantías",
        _ => AffiliationType
    };
    
    public string StatusDisplay => Status switch
    {
        "active" => "Activo",
        "inactive" => "Inactivo",
        "pending" => "Pendiente",
        "suspended" => "Suspendido",
        _ => Status
    };
}

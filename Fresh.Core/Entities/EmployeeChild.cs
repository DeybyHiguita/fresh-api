namespace Fresh.Core.Entities;

/// <summary>
/// Hijos de los empleados
/// </summary>
public class EmployeeChild
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    
    // Información del hijo
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? DocumentType { get; set; }
    public string? DocumentNumber { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? Gender { get; set; }
    
    // Información adicional
    public bool IsStudent { get; set; } = false;
    public string? SchoolName { get; set; }
    public bool HasDisability { get; set; } = false;
    public string? DisabilityType { get; set; }
    
    // Auditoría
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navegación
    public Employee? Employee { get; set; }
    public ICollection<EmployeeChildDocument>? Documents { get; set; }
    
    // Computed
    public string FullName => $"{FirstName} {LastName}";
    
    public int? Age => BirthDate.HasValue 
        ? (int)((DateTime.UtcNow - BirthDate.Value).TotalDays / 365.25) 
        : null;
}

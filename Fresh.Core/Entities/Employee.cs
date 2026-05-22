namespace Fresh.Core.Entities;

/// <summary>
/// Información personal y laboral del empleado
/// </summary>
public class Employee
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    
    // Información personal
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DocumentType { get; set; } = "CC";
    public string DocumentNumber { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public string? Gender { get; set; }
    public string? MaritalStatus { get; set; }
    public string? BloodType { get; set; }
    
    // Contacto
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? PersonalEmail { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    
    // Dirección
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Department { get; set; }
    public string? Neighborhood { get; set; }
    public string? PostalCode { get; set; }
    
    // Información laboral
    public string? Position { get; set; }
    public DateTime? HireDate { get; set; }
    public string? ContractType { get; set; }
    public decimal? Salary { get; set; }
    public string PaymentFrequency { get; set; } = "monthly";
    public string? BankName { get; set; }
    public string? BankAccountType { get; set; }
    public string? BankAccountNumber { get; set; }
    
    // Estado
    public bool IsActive { get; set; } = true;
    public DateTime? TerminationDate { get; set; }
    public string? TerminationReason { get; set; }
    
    // Auditoría
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navegación
    public User? User { get; set; }
    public ICollection<EmployeeDocument>? Documents { get; set; }
    public ICollection<EmployeeChild>? Children { get; set; }
    public ICollection<EmployeeAffiliation>? Affiliations { get; set; }
    
    // Computed
    public string FullName => $"{FirstName} {LastName}";
}

namespace Fresh.Core.DTOs.Employee;

/// <summary>
/// Response con información del empleado
/// </summary>
public class EmployeeResponse
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
    
    // Información personal
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
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
    public string PaymentFrequency { get; set; } = string.Empty;
    public string? BankName { get; set; }
    public string? BankAccountType { get; set; }
    public string? BankAccountNumber { get; set; }
    
    // Estado
    public bool IsActive { get; set; }
    public DateTime? TerminationDate { get; set; }
    public string? TerminationReason { get; set; }
    
    // Contadores
    public int DocumentsCount { get; set; }
    public int ChildrenCount { get; set; }
    
    // Auditoría
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

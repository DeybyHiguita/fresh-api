namespace Fresh.Core.DTOs.Employee;

/// <summary>
/// Request para crear/actualizar empleado
/// </summary>
public class EmployeeRequest
{
    // Vinculación de usuario
    public int? UserId { get; set; }
    /// <summary>Cuando es true, crea un nuevo usuario y lo vincula al empleado</summary>
    public bool CreateUser { get; set; } = false;
    /// <summary>Contraseña para el nuevo usuario (requerida si CreateUser = true)</summary>
    public string? UserPassword { get; set; }
    /// <summary>Rol del nuevo usuario (por defecto "employee")</summary>
    public string? UserRole { get; set; } = "employee";
    
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
    public string? Email { get; set; }         // Email principal (puede usarse como PersonalEmail)
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
}

namespace Fresh.Core.DTOs.EmployeeChild;

public class EmployeeChildResponse
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? DocumentType { get; set; }
    public string? DocumentNumber { get; set; }
    public DateTime? BirthDate { get; set; }
    public int? Age { get; set; }
    public string? Gender { get; set; }
    
    public bool IsStudent { get; set; }
    public string? SchoolName { get; set; }
    public bool HasDisability { get; set; }
    public string? DisabilityType { get; set; }
    
    public bool IsActive { get; set; }
    public int DocumentsCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

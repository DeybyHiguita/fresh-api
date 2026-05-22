namespace Fresh.Core.DTOs.EmployeeChild;

public class EmployeeChildRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? DocumentType { get; set; }
    public string? DocumentNumber { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? Gender { get; set; }
    public bool IsStudent { get; set; } = false;
    public string? SchoolName { get; set; }
    public bool HasDisability { get; set; } = false;
    public string? DisabilityType { get; set; }
}

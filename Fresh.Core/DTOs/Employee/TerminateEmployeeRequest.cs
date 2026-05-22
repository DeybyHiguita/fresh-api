namespace Fresh.Core.DTOs.Employee;

/// <summary>
/// Request para dar de baja a un empleado
/// </summary>
public class TerminateEmployeeRequest
{
    public DateTime TerminationDate { get; set; }
    public string? TerminationReason { get; set; }
}

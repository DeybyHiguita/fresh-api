namespace Fresh.Core.DTOs.User;

public class UserResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    
    /// <summary>
    /// Indica si este usuario ya tiene un empleado vinculado
    /// </summary>
    public bool HasEmployee { get; set; }
    
    /// <summary>
    /// ID del empleado vinculado (si existe)
    /// </summary>
    public int? EmployeeId { get; set; }
}

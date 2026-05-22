namespace Fresh.Core.DTOs.Employee;

/// <summary>
/// Request para vincular empleado con usuario
/// </summary>
public class LinkUserRequest
{
    /// <summary>
    /// ID del usuario existente (si se quiere vincular uno existente)
    /// </summary>
    public int? UserId { get; set; }
    
    /// <summary>
    /// Si true, crea un nuevo usuario con los datos proporcionados
    /// </summary>
    public bool CreateNewUser { get; set; } = false;
    
    /// <summary>
    /// Email para el nuevo usuario (requerido si CreateNewUser = true)
    /// </summary>
    public string? Email { get; set; }
    
    /// <summary>
    /// Contraseña para el nuevo usuario (requerido si CreateNewUser = true)
    /// </summary>
    public string? Password { get; set; }
    
    /// <summary>
    /// Rol para el nuevo usuario
    /// </summary>
    public string Role { get; set; } = "employee";
}

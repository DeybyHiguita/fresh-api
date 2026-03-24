using System.ComponentModel.DataAnnotations;

namespace Fresh.Core.DTOs.User;

public class UserUpdateRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Opcional. Si viene vacío, no se actualiza la contraseña.</summary>
    public string? Password { get; set; }

    [Required]
    [MaxLength(20)]
    public string Role { get; set; } = "employee";

    public bool IsActive { get; set; } = true;
}

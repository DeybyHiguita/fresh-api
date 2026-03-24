using System.ComponentModel.DataAnnotations;

namespace Fresh.Core.DTOs.UserSession;

public class StartSessionRequest
{
    [Required]
    public int UserId { get; set; }
    public string? ConnectionId { get; set; }
    public string? Location { get; set; }
}

public class UpdateLocationRequest
{
    [Required]
    [MaxLength(200)]
    public string Location { get; set; } = string.Empty;
}

public class UpdateIdleRequest
{
    [Range(0, int.MaxValue)]
    public int IdleSeconds { get; set; }
}

public class LogActionRequest
{
    [Required]
    [MaxLength(50)]
    public string ActionType { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
}

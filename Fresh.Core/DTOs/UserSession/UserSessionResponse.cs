namespace Fresh.Core.DTOs.UserSession;

public class UserSessionResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string ConnectionId { get; set; } = string.Empty;
    public DateTimeOffset ConnectedAt { get; set; }
    public DateTimeOffset? DisconnectedAt { get; set; }
    public int TotalIdleSeconds { get; set; }
    public string? LastKnownLocation { get; set; }

    // Propiedad calculada útil para el frontend
    public bool IsOnline => !DisconnectedAt.HasValue;
}
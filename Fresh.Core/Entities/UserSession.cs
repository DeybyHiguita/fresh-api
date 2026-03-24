namespace Fresh.Core.Entities;

public class UserSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? ConnectionId { get; set; }
    public DateTimeOffset ConnectedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DisconnectedAt { get; set; }
    public int TotalIdleSeconds { get; set; } = 0;
    public string? LastKnownLocation { get; set; }
    public bool IsOnline { get; set; } = true;

    public User? User { get; set; }
    public ICollection<UserAction> Actions { get; set; } = [];
}

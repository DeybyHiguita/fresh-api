namespace Fresh.Core.Entities;

public class UserAction
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public UserSession? Session { get; set; }
}

namespace Fresh.Core.DTOs.UserSession;

public class UserActionResponse
{
    public int Id { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
namespace Fresh.Core.Entities;

public class AppAlert
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    /// <summary>info | warning | urgent</summary>
    public string AlertType { get; set; } = "info";
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastSentAt { get; set; }
    public int SendCount { get; set; } = 0;
}

namespace Fresh.Core.DTOs.Alert;

public class AlertRequest
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    /// <summary>info | warning | urgent</summary>
    public string AlertType { get; set; } = "info";
}

public class AlertResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string AlertType { get; set; } = string.Empty;
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastSentAt { get; set; }
    public int SendCount { get; set; }
}

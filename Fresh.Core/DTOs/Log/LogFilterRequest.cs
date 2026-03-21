namespace Fresh.Core.DTOs.Log;

public class LogFilterRequest
{
    public string? LogLevel { get; set; }
    public string? EntityName { get; set; }
    public string? UserId { get; set; }
    public string? TransactionStatus { get; set; }
    public string? TransactionId { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

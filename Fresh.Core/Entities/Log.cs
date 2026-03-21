namespace Fresh.Core.Entities;

public class Log
{
    public long Id { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public DateTimeOffset LogDate { get; set; } = DateTimeOffset.UtcNow;
    public string LogLevel { get; set; } = string.Empty;
    public string? Operation { get; set; }
    public string? EntityName { get; set; }
    public string? EntityId { get; set; }
    public string? UserId { get; set; }
    public string? TransactionStatus { get; set; }
    public int? DurationMs { get; set; }
    public string? Logger { get; set; }
    public string? Message { get; set; }
    public string? Exception { get; set; }
    public string? TransactionData { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

using System.ComponentModel.DataAnnotations;

namespace Fresh.Core.DTOs.Log;

public class LogRequest
{
    [Required]
    [MaxLength(100)]
    public string TransactionId { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? CorrelationId { get; set; }

    [Required]
    [MaxLength(20)]
    public string LogLevel { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Operation { get; set; }

    [MaxLength(100)]
    public string? EntityName { get; set; }

    [MaxLength(100)]
    public string? EntityId { get; set; }

    [MaxLength(100)]
    public string? UserId { get; set; }

    [MaxLength(30)]
    public string? TransactionStatus { get; set; }

    public int? DurationMs { get; set; }

    [MaxLength(255)]
    public string? Logger { get; set; }

    public string? Message { get; set; }
    public string? Exception { get; set; }
    public string? TransactionData { get; set; }
}

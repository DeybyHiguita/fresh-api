using System.ComponentModel.DataAnnotations;

namespace Fresh.Core.DTOs.BreakTime;

public class BreakTimeRequest
{
    [Required]
    public DateTimeOffset StartTime { get; set; }

    public DateTimeOffset? EndTime { get; set; }
}

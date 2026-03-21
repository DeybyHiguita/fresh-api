using System.ComponentModel.DataAnnotations;

namespace Fresh.Core.DTOs.WorkShift;

public class WorkShiftRequest
{
    [Required]
    public int UserId { get; set; }

    public DateOnly? ShiftDate { get; set; }

    [Required]
    public DateTimeOffset StartTime { get; set; }

    public DateTimeOffset? EndTime { get; set; }
}

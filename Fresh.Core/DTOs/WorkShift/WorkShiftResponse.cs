using Fresh.Core.DTOs.BreakTime;

namespace Fresh.Core.DTOs.WorkShift;

public class WorkShiftResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateOnly ShiftDate { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public decimal? TotalHours { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<BreakTimeResponse> BreakTimes { get; set; } = [];
}

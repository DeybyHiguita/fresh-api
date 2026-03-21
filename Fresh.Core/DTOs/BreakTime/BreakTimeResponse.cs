namespace Fresh.Core.DTOs.BreakTime;

public class BreakTimeResponse
{
    public int Id { get; set; }
    public int ShiftId { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

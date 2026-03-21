namespace Fresh.Core.Entities;

public class BreakTime
{
    public int Id { get; set; }
    public int ShiftId { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Relaciones
    public WorkShift Shift { get; set; } = null!;
}

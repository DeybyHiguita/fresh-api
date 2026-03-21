namespace Fresh.Core.Entities;

public class WorkShift
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateOnly ShiftDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public decimal? TotalHours { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Relaciones
    public User User { get; set; } = null!;
    public ICollection<BreakTime> BreakTimes { get; set; } = [];
}

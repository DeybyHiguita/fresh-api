namespace Fresh.Core.DTOs.WorkShift;

public class DailyWorkedHoursResponse
{
    public int ShiftId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateOnly ShiftDate { get; set; }
    public DateTimeOffset ShiftStart { get; set; }
    public DateTimeOffset? ShiftEnd { get; set; }
    public decimal GrossHours { get; set; }
    public decimal TotalBreakHours { get; set; }
    public decimal NetWorkedHours { get; set; }
}

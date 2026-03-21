namespace Fresh.Core.Entities;

/// <summary>
/// Entidad sin clave mapeada a la vista vw_daily_worked_hours (solo lectura).
/// </summary>
public class DailyWorkedHours
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

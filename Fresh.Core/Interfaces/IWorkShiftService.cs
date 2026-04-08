using Fresh.Core.DTOs.BreakTime;
using Fresh.Core.DTOs.WorkShift;

namespace Fresh.Core.Interfaces;

public interface IWorkShiftService
{
    Task<IEnumerable<WorkShiftResponse>> GetAllAsync(int? userId = null, DateOnly? date = null);
    Task<WorkShiftResponse?> GetByIdAsync(int id);
    Task<WorkShiftResponse> StartShiftAsync(WorkShiftRequest request);
    Task<WorkShiftResponse?> EndShiftAsync(int id);
    Task<WorkShiftResponse?> UpdateAsync(int id, WorkShiftRequest request);
    Task<bool> DeleteAsync(int id);

    // Descansos
    Task<BreakTimeResponse> StartBreakAsync(int shiftId, BreakTimeRequest request);
    Task<BreakTimeResponse> AddBreakAdminAsync(int shiftId, BreakTimeRequest request);
    Task<BreakTimeResponse?> UpdateBreakAsync(int shiftId, int breakId, BreakTimeRequest request);
    Task<BreakTimeResponse?> EndBreakAsync(int shiftId, int breakId);
    Task<bool> RemoveBreakAsync(int shiftId, int breakId);

    // Vista de horas trabajadas
    Task<IEnumerable<DailyWorkedHoursResponse>> GetDailyWorkedHoursAsync(int? userId = null, DateOnly? date = null);
}

using Fresh.Core.DTOs.BreakTime;
using Fresh.Core.DTOs.WorkShift;
using Fresh.Core.Entities;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fresh.Infrastructure.Services;

public class WorkShiftService : IWorkShiftService
{
    private readonly FreshDbContext _context;

    public WorkShiftService(FreshDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<WorkShiftResponse>> GetAllAsync(int? userId = null, DateOnly? date = null)
    {
        var query = _context.WorkShifts
            .Include(s => s.User)
            .Include(s => s.BreakTimes)
            .AsQueryable();

        if (userId.HasValue)
            query = query.Where(s => s.UserId == userId.Value);

        if (date.HasValue)
            query = query.Where(s => s.ShiftDate == date.Value);

        var shifts = await query
            .OrderByDescending(s => s.ShiftDate)
            .ThenByDescending(s => s.StartTime)
            .ToListAsync();

        return shifts.Select(MapToResponse);
    }

    public async Task<WorkShiftResponse?> GetByIdAsync(int id)
    {
        var shift = await _context.WorkShifts
            .Include(s => s.User)
            .Include(s => s.BreakTimes)
            .FirstOrDefaultAsync(s => s.Id == id);

        return shift == null ? null : MapToResponse(shift);
    }

    public async Task<WorkShiftResponse> StartShiftAsync(WorkShiftRequest request)
    {
        var userExists = await _context.Users.AnyAsync(u => u.Id == request.UserId);
        if (!userExists)
            throw new KeyNotFoundException($"El usuario con ID {request.UserId} no existe");

        // Validar que no tenga una jornada abierta en la misma fecha
        var shiftDate = request.ShiftDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var openShift = await _context.WorkShifts
            .AnyAsync(s => s.UserId == request.UserId
                        && s.ShiftDate == shiftDate
                        && s.EndTime == null);

        if (openShift)
            throw new InvalidOperationException("El usuario ya tiene una jornada en curso para esta fecha");

        var shift = new WorkShift
        {
            UserId = request.UserId,
            ShiftDate = shiftDate,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            TotalHours = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.WorkShifts.Add(shift);
        await _context.SaveChangesAsync();

        return (await GetByIdAsync(shift.Id))!;
    }

    public async Task<WorkShiftResponse?> EndShiftAsync(int id)
    {
        var shift = await _context.WorkShifts
            .Include(s => s.User)
            .Include(s => s.BreakTimes)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (shift == null) return null;

        if (shift.EndTime.HasValue)
            throw new InvalidOperationException("La jornada ya fue finalizada");

        // Cerrar cualquier descanso abierto autom�ticamente
        var openBreak = shift.BreakTimes.FirstOrDefault(b => b.EndTime == null);
        if (openBreak != null)
        {
            openBreak.EndTime = DateTimeOffset.UtcNow;
            openBreak.UpdatedAt = DateTime.UtcNow;
        }

        shift.EndTime = DateTimeOffset.UtcNow;

        // Calcular horas totales netas
        var grossHours = (shift.EndTime.Value - shift.StartTime).TotalHours;
        var breakHours = shift.BreakTimes
            .Where(b => b.EndTime.HasValue)
            .Sum(b => (b.EndTime!.Value - b.StartTime).TotalHours);

        shift.TotalHours = (decimal)(grossHours - breakHours);
        shift.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToResponse(shift);
    }

    public async Task<WorkShiftResponse?> UpdateAsync(int id, WorkShiftRequest request)
    {
        var shift = await _context.WorkShifts
            .Include(s => s.User)
            .Include(s => s.BreakTimes)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (shift == null) return null;

        shift.UserId = request.UserId;
        shift.ShiftDate = request.ShiftDate ?? shift.ShiftDate;
        shift.StartTime = request.StartTime;
        shift.EndTime = request.EndTime;
        shift.UpdatedAt = DateTime.UtcNow;

        RecalculateNetHours(shift);
        await _context.SaveChangesAsync();

        return MapToResponse(shift);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var shift = await _context.WorkShifts.FindAsync(id);
        if (shift == null) return false;

        _context.WorkShifts.Remove(shift);
        await _context.SaveChangesAsync();

        return true;
    }

    // ?? Descansos ?????????????????????????????????????????????????????????????

    public async Task<BreakTimeResponse> StartBreakAsync(int shiftId, BreakTimeRequest request)
    {
        var shift = await _context.WorkShifts
            .Include(s => s.BreakTimes)
            .FirstOrDefaultAsync(s => s.Id == shiftId);

        if (shift == null)
            throw new KeyNotFoundException($"La jornada con ID {shiftId} no existe");

        if (shift.EndTime.HasValue)
            throw new InvalidOperationException("No se puede iniciar un descanso en una jornada finalizada");

        var openBreak = shift.BreakTimes.Any(b => b.EndTime == null);
        if (openBreak)
            throw new InvalidOperationException("Ya hay un descanso en curso para esta jornada");

        var breakTime = new BreakTime
        {
            ShiftId = shiftId,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.BreakTimes.Add(breakTime);
        await _context.SaveChangesAsync();

        return MapBreakToResponse(breakTime);
    }

    public async Task<BreakTimeResponse> AddBreakAdminAsync(int shiftId, BreakTimeRequest request)
    {
        var shift = await _context.WorkShifts
            .Include(s => s.BreakTimes)
            .FirstOrDefaultAsync(s => s.Id == shiftId);

        if (shift == null)
            throw new KeyNotFoundException($"La jornada con ID {shiftId} no existe");

        if (!request.EndTime.HasValue)
            throw new InvalidOperationException("Se requiere hora de fin para agregar un descanso retroactivo");

        var breakTime = new BreakTime
        {
            ShiftId = shiftId,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        shift.BreakTimes.Add(breakTime);
        RecalculateNetHours(shift);
        await _context.SaveChangesAsync();

        return MapBreakToResponse(breakTime);
    }

    public async Task<BreakTimeResponse?> UpdateBreakAsync(int shiftId, int breakId, BreakTimeRequest request)
    {
        var shift = await _context.WorkShifts
            .Include(s => s.BreakTimes)
            .FirstOrDefaultAsync(s => s.Id == shiftId);

        if (shift == null) return null;

        var breakTime = shift.BreakTimes.FirstOrDefault(b => b.Id == breakId);
        if (breakTime == null) return null;

        breakTime.StartTime = request.StartTime;
        if (request.EndTime.HasValue)
            breakTime.EndTime = request.EndTime;
        breakTime.UpdatedAt = DateTime.UtcNow;

        RecalculateNetHours(shift);
        await _context.SaveChangesAsync();

        return MapBreakToResponse(breakTime);
    }

    public async Task<BreakTimeResponse?> EndBreakAsync(int shiftId, int breakId)
    {
        var shift = await _context.WorkShifts
            .Include(s => s.BreakTimes)
            .FirstOrDefaultAsync(s => s.Id == shiftId);

        if (shift == null) return null;

        var breakTime = shift.BreakTimes.FirstOrDefault(b => b.Id == breakId);
        if (breakTime == null) return null;

        if (breakTime.EndTime.HasValue)
            throw new InvalidOperationException("El descanso ya fue finalizado");

        breakTime.EndTime = DateTimeOffset.UtcNow;
        breakTime.UpdatedAt = DateTime.UtcNow;

        RecalculateNetHours(shift);
        await _context.SaveChangesAsync();

        return MapBreakToResponse(breakTime);
    }

    public async Task<bool> RemoveBreakAsync(int shiftId, int breakId)
    {
        var shift = await _context.WorkShifts
            .Include(s => s.BreakTimes)
            .FirstOrDefaultAsync(s => s.Id == shiftId);

        if (shift == null) return false;

        var breakTime = shift.BreakTimes.FirstOrDefault(b => b.Id == breakId);
        if (breakTime == null) return false;

        shift.BreakTimes.Remove(breakTime);
        _context.BreakTimes.Remove(breakTime);
        RecalculateNetHours(shift);
        await _context.SaveChangesAsync();

        return true;
    }

    // ?? Vista horas trabajadas ????????????????????????????????????????????????

    public async Task<IEnumerable<DailyWorkedHoursResponse>> GetDailyWorkedHoursAsync(
        int? userId = null, DateOnly? date = null)
    {
        var query = _context.DailyWorkedHours.AsQueryable();

        if (userId.HasValue)
            query = query.Where(v => v.UserId == userId.Value);

        if (date.HasValue)
            query = query.Where(v => v.ShiftDate == date.Value);

        var results = await query
            .OrderByDescending(v => v.ShiftDate)
            .ToListAsync();

        return results.Select(v => new DailyWorkedHoursResponse
        {
            ShiftId = v.ShiftId,
            UserId = v.UserId,
            UserName = v.UserName,
            ShiftDate = v.ShiftDate,
            ShiftStart = v.ShiftStart,
            ShiftEnd = v.ShiftEnd,
            GrossHours = v.GrossHours,
            TotalBreakHours = v.TotalBreakHours,
            NetWorkedHours = v.NetWorkedHours
        });
    }

    // ?? Helpers ???????????????????????????????????????????????????????????????

    private static void RecalculateNetHours(WorkShift shift)
    {
        if (shift.EndTime == null) return;
        var grossHours = (shift.EndTime.Value - shift.StartTime).TotalHours;
        var breakHours = shift.BreakTimes
            .Where(b => b.EndTime.HasValue)
            .Sum(b => (b.EndTime!.Value - b.StartTime).TotalHours);
        shift.TotalHours = (decimal)Math.Max(0, grossHours - breakHours);
    }

    private static WorkShiftResponse MapToResponse(WorkShift shift) => new()
    {
        Id = shift.Id,
        UserId = shift.UserId,
        UserName = shift.User?.Name ?? string.Empty,
        ShiftDate = shift.ShiftDate,
        StartTime = shift.StartTime,
        EndTime = shift.EndTime,
        TotalHours = shift.TotalHours,
        IsActive = shift.EndTime == null,
        CreatedAt = shift.CreatedAt,
        UpdatedAt = shift.UpdatedAt,
        BreakTimes = shift.BreakTimes.Select(MapBreakToResponse).ToList()
    };

    private static BreakTimeResponse MapBreakToResponse(BreakTime b) => new()
    {
        Id = b.Id,
        ShiftId = b.ShiftId,
        StartTime = b.StartTime,
        EndTime = b.EndTime,
        IsActive = b.EndTime == null,
        CreatedAt = b.CreatedAt,
        UpdatedAt = b.UpdatedAt
    };
}

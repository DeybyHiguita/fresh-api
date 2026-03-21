using Fresh.Core.DTOs.BreakTime;
using Fresh.Core.DTOs.WorkShift;
using Fresh.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fresh.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WorkShiftsController : ControllerBase
{
    private readonly IWorkShiftService _workShiftService;

    public WorkShiftsController(IWorkShiftService workShiftService)
    {
        _workShiftService = workShiftService;
    }

    /// <summary>
    /// Obtiene todas las jornadas. Filtra opcionalmente por usuario y/o fecha.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<WorkShiftResponse>>> GetAll(
        [FromQuery] int? userId,
        [FromQuery] DateOnly? date)
    {
        var shifts = await _workShiftService.GetAllAsync(userId, date);
        return Ok(shifts);
    }

    /// <summary>
    /// Obtiene una jornada por ID con sus descansos
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<WorkShiftResponse>> GetById(int id)
    {
        var shift = await _workShiftService.GetByIdAsync(id);
        if (shift == null)
            return NotFound(new { message = "Jornada no encontrada" });

        return Ok(shift);
    }

    /// <summary>
    /// Inicia una nueva jornada laboral para un usuario
    /// </summary>
    [HttpPost("start")]
    public async Task<ActionResult<WorkShiftResponse>> StartShift([FromBody] WorkShiftRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var shift = await _workShiftService.StartShiftAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = shift.Id }, shift);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Finaliza una jornada activa. Calcula automáticamente las horas netas.
    /// </summary>
    [HttpPatch("{id}/end")]
    public async Task<ActionResult<WorkShiftResponse>> EndShift(int id)
    {
        try
        {
            var shift = await _workShiftService.EndShiftAsync(id);
            if (shift == null)
                return NotFound(new { message = "Jornada no encontrada" });

            return Ok(shift);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Actualiza los datos de una jornada
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<WorkShiftResponse>> Update(int id, [FromBody] WorkShiftRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var shift = await _workShiftService.UpdateAsync(id, request);
        if (shift == null)
            return NotFound(new { message = "Jornada no encontrada" });

        return Ok(shift);
    }

    /// <summary>
    /// Elimina una jornada y sus descansos (CASCADE)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _workShiftService.DeleteAsync(id);
        if (!result)
            return NotFound(new { message = "Jornada no encontrada" });

        return NoContent();
    }

    // ?? Descansos (anidados bajo la jornada) ??????????????????????????????????

    /// <summary>
    /// Inicia un descanso dentro de una jornada activa
    /// </summary>
    [HttpPost("{id}/breaks/start")]
    public async Task<ActionResult<BreakTimeResponse>> StartBreak(int id, [FromBody] BreakTimeRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var breakTime = await _workShiftService.StartBreakAsync(id, request);
            return CreatedAtAction(nameof(GetById), new { id }, breakTime);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Finaliza el descanso activo de una jornada
    /// </summary>
    [HttpPatch("{id}/breaks/{breakId}/end")]
    public async Task<ActionResult<BreakTimeResponse>> EndBreak(int id, int breakId)
    {
        try
        {
            var breakTime = await _workShiftService.EndBreakAsync(id, breakId);
            if (breakTime == null)
                return NotFound(new { message = "Descanso no encontrado" });

            return Ok(breakTime);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Elimina un descanso de una jornada
    /// </summary>
    [HttpDelete("{id}/breaks/{breakId}")]
    public async Task<IActionResult> RemoveBreak(int id, int breakId)
    {
        var result = await _workShiftService.RemoveBreakAsync(id, breakId);
        if (!result)
            return NotFound(new { message = "Descanso no encontrado" });

        return NoContent();
    }

    // ?? Vista horas trabajadas ????????????????????????????????????????????????

    /// <summary>
    /// Consulta la vista vw_daily_worked_hours con horas brutas, de descanso y netas
    /// </summary>
    [HttpGet("daily-hours")]
    public async Task<ActionResult<IEnumerable<DailyWorkedHoursResponse>>> GetDailyHours(
        [FromQuery] int? userId,
        [FromQuery] DateOnly? date)
    {
        var results = await _workShiftService.GetDailyWorkedHoursAsync(userId, date);
        return Ok(results);
    }
}

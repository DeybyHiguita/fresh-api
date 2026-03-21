using Fresh.Core.DTOs.Log;
using Fresh.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fresh.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LogsController : ControllerBase
{
    private readonly ILogService _logService;

    public LogsController(ILogService logService)
    {
        _logService = logService;
    }

    /// <summary>
    /// Obtiene todos los logs con filtros opcionales y paginación
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedLogResponse>> GetAll([FromQuery] LogFilterRequest filter)
    {
        var result = await _logService.GetAllAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Obtiene un log por su ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<LogResponse>> GetById(long id)
    {
        var log = await _logService.GetByIdAsync(id);
        if (log == null)
            return NotFound(new { message = "Log no encontrado" });

        return Ok(log);
    }

    /// <summary>
    /// Registra un nuevo log
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<LogResponse>> Create([FromBody] LogRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var log = await _logService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = log.Id }, log);
    }

    /// <summary>
    /// Elimina un log por su ID
    /// </summary>
    [Authorize(Roles = "admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        var result = await _logService.DeleteAsync(id);
        if (!result)
            return NotFound(new { message = "Log no encontrado" });

        return NoContent();
    }
}

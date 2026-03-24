using Fresh.Core.DTOs.UserSession;
using Fresh.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fresh.Api.Controllers;

[ApiController]
[Route("api/user-sessions")]
[Authorize]
public class UserSessionsController : ControllerBase
{
    private readonly IUserSessionService _service;

    public UserSessionsController(IUserSessionService service) { _service = service; }

    /// <summary>Obtiene todas las sesiones actualmente en línea.</summary>
    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<UserSessionResponse>>> GetActive()
        => Ok(await _service.GetActiveSessionsAsync());

    /// <summary>Historial de sesiones de un usuario.</summary>
    [HttpGet("user/{userId}/history")]
    public async Task<ActionResult<IEnumerable<UserSessionResponse>>> GetHistory(int userId)
        => Ok(await _service.GetHistoryByUserAsync(userId));

    /// <summary>Acciones registradas dentro de una sesión.</summary>
    [HttpGet("{sessionId}/actions")]
    public async Task<ActionResult<IEnumerable<UserActionResponse>>> GetActions(int sessionId)
        => Ok(await _service.GetSessionActionsAsync(sessionId));

    /// <summary>Inicia una nueva sesión para un usuario.</summary>
    [HttpPost]
    public async Task<ActionResult<UserSessionResponse>> Start([FromBody] StartSessionRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var session = await _service.StartSessionAsync(request);
        return CreatedAtAction(nameof(GetActive), session);
    }

    /// <summary>Cierra una sesión activa.</summary>
    [HttpPut("{sessionId}/end")]
    public async Task<ActionResult<UserSessionResponse>> End(int sessionId)
    {
        var session = await _service.EndSessionAsync(sessionId);
        return session == null ? NotFound() : Ok(session);
    }

    /// <summary>Actualiza la última pantalla visitada.</summary>
    [HttpPut("{sessionId}/location")]
    public async Task<ActionResult<UserSessionResponse>> UpdateLocation(
        int sessionId, [FromBody] UpdateLocationRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var session = await _service.UpdateLocationAsync(sessionId, request);
        return session == null ? NotFound() : Ok(session);
    }

    /// <summary>Acumula segundos de inactividad.</summary>
    [HttpPut("{sessionId}/idle")]
    public async Task<ActionResult<UserSessionResponse>> AddIdle(
        int sessionId, [FromBody] UpdateIdleRequest request)
    {
        var session = await _service.AddIdleTimeAsync(sessionId, request);
        return session == null ? NotFound() : Ok(session);
    }

    /// <summary>Registra una acción dentro de la sesión.</summary>
    [HttpPost("{sessionId}/actions")]
    public async Task<ActionResult<UserActionResponse>> LogAction(
        int sessionId, [FromBody] LogActionRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var action = await _service.LogActionAsync(sessionId, request);
        return CreatedAtAction(nameof(GetActions), new { sessionId }, action);
    }
}

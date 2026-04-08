using Fresh.Api.Hubs;
using Fresh.Core.DTOs.Alert;
using Fresh.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Fresh.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin")]
public class AlertsController : ControllerBase
{
    private readonly IAlertService _alertService;
    private readonly IHubContext<OrderHub> _hub;

    public AlertsController(IAlertService alertService, IHubContext<OrderHub> hub)
    {
        _alertService = alertService;
        _hub          = hub;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AlertResponse>>> GetAll()
        => Ok(await _alertService.GetAllAsync());

    [HttpPost]
    public async Task<ActionResult<AlertResponse>> Create([FromBody] AlertRequest request)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0";
        var userId    = int.TryParse(userIdStr, out var id) ? id : 0;
        var alert     = await _alertService.CreateAsync(request, userId);
        return CreatedAtAction(nameof(GetAll), new { id = alert.Id }, alert);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AlertResponse>> Update(int id, [FromBody] AlertRequest request)
    {
        var result = await _alertService.UpdateAsync(id, request);
        if (result == null) return NotFound(new { message = "Alerta no encontrada" });
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _alertService.DeleteAsync(id);
        if (!ok) return NotFound(new { message = "Alerta no encontrada" });
        return NoContent();
    }

    /// <summary>Envía la alerta por SignalR a todos los usuarios no-admin conectados.</summary>
    [HttpPost("{id}/send")]
    public async Task<ActionResult<AlertResponse>> Send(int id)
    {
        var alert = await _alertService.MarkSentAsync(id);
        if (alert == null) return NotFound(new { message = "Alerta no encontrada" });

        await _hub.Clients.Group("users").SendAsync("ReceiveAlert", alert);
        return Ok(alert);
    }
}

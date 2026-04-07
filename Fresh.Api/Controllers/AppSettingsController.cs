using Fresh.Core.DTOs.AppSettings;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fresh.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppSettingsController : ControllerBase
{
    private readonly IAppSettingsService _service;
    private readonly WhatsAppNotificationService _whatsApp;

    public AppSettingsController(IAppSettingsService service, WhatsAppNotificationService whatsApp)
    {
        _service  = service;
        _whatsApp = whatsApp;
    }

    /// <summary>Obtiene la configuración actual. Accesible para usuarios autenticados.</summary>
    [Authorize]
    [HttpGet]
    public async Task<ActionResult<AppSettingsResponse>> Get()
    {
        return Ok(await _service.GetAsync());
    }

    /// <summary>Actualiza la configuración. Solo admins.</summary>
    [Authorize(Roles = "admin")]
    [HttpPut]
    public async Task<ActionResult<AppSettingsResponse>> Update([FromBody] UpdateAppSettingsRequest request)
    {
        return Ok(await _service.UpdateAsync(request));
    }

    /// <summary>Envía hello_world para verificar que la configuración de WhatsApp funciona.</summary>
    [Authorize(Roles = "admin")]
    [HttpPost("whatsapp-test")]
    public async Task<IActionResult> WhatsAppTest()
    {
        var result = await _whatsApp.SendHelloWorldAsync();
        if (result.Success)
            return Ok(new { message = "Mensaje enviado correctamente." });

        return BadRequest(new { message = result.Error });
    }
}

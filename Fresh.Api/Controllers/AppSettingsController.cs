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
    private readonly IHttpClientFactory _httpClientFactory;

    public AppSettingsController(
        IAppSettingsService service,
        WhatsAppNotificationService whatsApp,
        IHttpClientFactory httpClientFactory)
    {
        _service  = service;
        _whatsApp = whatsApp;
        _httpClientFactory = httpClientFactory;
    }

    public record GeminiTestRequest(string? ApiKey);

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

    /// <summary>
    /// Valida una API key de Gemini. Si el body trae una key la prueba; si no, usa la guardada.
    /// </summary>
    [Authorize(Roles = "admin")]
    [HttpPost("gemini-test")]
    public async Task<IActionResult> GeminiTest([FromBody] GeminiTestRequest? request)
    {
        var apiKey = !string.IsNullOrWhiteSpace(request?.ApiKey)
            ? request!.ApiKey!.Trim()
            : await _service.GetGeminiApiKeyAsync();

        if (string.IsNullOrWhiteSpace(apiKey))
            return BadRequest(new { valid = false, message = "No hay una API key configurada para validar." });

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(15);
            var url = $"https://generativelanguage.googleapis.com/v1beta/models?key={apiKey}";
            var resp = await client.GetAsync(url);

            if (resp.IsSuccessStatusCode)
                return Ok(new { valid = true, message = "La API key de Gemini es válida ✓" });

            var status = (int)resp.StatusCode;
            var msg = status is 400 or 401 or 403
                ? "La API key no es válida o no tiene permisos."
                : $"Gemini respondió con estado {status}.";
            return Ok(new { valid = false, message = msg });
        }
        catch (Exception ex)
        {
            return Ok(new { valid = false, message = $"No se pudo validar: {ex.Message}" });
        }
    }
}

using Fresh.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Fresh.Api.Controllers;

/// <summary>
/// Endpoint de webhook para Meta WhatsApp Business Cloud API.
/// GET  /api/whatsapp/webhook  → verificación de Meta (challenge)
/// POST /api/whatsapp/webhook  → recibe mensajes, estados y eventos
/// </summary>
[ApiController]
[Route("api/whatsapp/webhook")]
public class WhatsAppWebhookController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly WhatsAppWebhookService _webhookService;
    private readonly ILogger<WhatsAppWebhookController> _logger;

    public WhatsAppWebhookController(
        IConfiguration config,
        WhatsAppWebhookService webhookService,
        ILogger<WhatsAppWebhookController> logger)
    {
        _config         = config;
        _webhookService = webhookService;
        _logger         = logger;
    }

    // ── Verificación de Meta ──────────────────────────────────────────────
    // Meta hace GET con hub.mode=subscribe, hub.verify_token y hub.challenge
    [HttpGet]
    public IActionResult Verify(
        [FromQuery(Name = "hub.mode")]         string? mode,
        [FromQuery(Name = "hub.verify_token")] string? token,
        [FromQuery(Name = "hub.challenge")]    string? challenge)
    {
        var expectedToken = _config["WhatsApp:VerifyToken"];

        if (mode == "subscribe" && token == expectedToken)
        {
            _logger.LogInformation("[WhatsApp Webhook] Verificación exitosa.");
            return Ok(challenge);
        }

        _logger.LogWarning("[WhatsApp Webhook] Verificación fallida. Token incorrecto.");
        return Forbid();
    }

    // ── Recepción de eventos ──────────────────────────────────────────────
    [HttpPost]
    public IActionResult Receive([FromBody] JsonElement payload, [FromServices] IServiceProvider services)
    {
        // IMPORTANTE: JsonElement es una referencia a memoria del JsonDocument del request.
        // Al devolver Ok() el scope HTTP se libera y esa memoria se invalida.
        // Clone() crea una copia independiente que vive mientras el Task.Run la necesite.
        var safePayload = payload.Clone();
        _ = Task.Run(() => _webhookService.ProcessAsync(safePayload, services));
        return Ok();
    }
}

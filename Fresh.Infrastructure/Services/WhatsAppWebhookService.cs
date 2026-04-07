using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Fresh.Infrastructure.Services;

/// <summary>
/// Procesa los payloads de webhook de Meta WhatsApp Business.
/// Corre en background (fire-and-forget) para devolver 200 OK a Meta de inmediato.
/// </summary>
public class WhatsAppWebhookService
{
    private readonly ILogger<WhatsAppWebhookService> _logger;

    public WhatsAppWebhookService(ILogger<WhatsAppWebhookService> logger)
    {
        _logger = logger;
    }

    public Task ProcessAsync(JsonElement payload)
    {
        try
        {
            var raw = payload.GetRawText();
            _logger.LogInformation("[WhatsApp Webhook] Payload recibido: {Payload}", raw);

            if (!payload.TryGetProperty("entry", out var entries)) return Task.CompletedTask;

            foreach (var entry in entries.EnumerateArray())
            {
                if (!entry.TryGetProperty("changes", out var changes)) continue;

                foreach (var change in changes.EnumerateArray())
                {
                    var field = change.TryGetProperty("field", out var f) ? f.GetString() : null;
                    if (field != "messages") continue;

                    if (!change.TryGetProperty("value", out var value)) continue;

                    // ── Mensajes entrantes ──────────────────────────────
                    if (value.TryGetProperty("messages", out var messages))
                    {
                        foreach (var msg in messages.EnumerateArray())
                        {
                            var from      = msg.TryGetProperty("from",      out var fr) ? fr.GetString() : "desconocido";
                            var msgType   = msg.TryGetProperty("type",      out var t)  ? t.GetString()  : "unknown";
                            var timestamp = msg.TryGetProperty("timestamp", out var ts) ? ts.GetString() : null;

                            if (msgType == "text" && msg.TryGetProperty("text", out var textObj))
                            {
                                var body = textObj.TryGetProperty("body", out var b) ? b.GetString() : "";
                                _logger.LogInformation("[WhatsApp] Mensaje de {From}: {Body}", from, body);
                                // TODO: aquí puedes guardar el mensaje en BD, notificar por SignalR, etc.
                            }
                            else
                            {
                                _logger.LogInformation("[WhatsApp] Mensaje tipo '{Type}' de {From}", msgType, from);
                            }
                        }
                    }

                    // ── Estados de mensajes salientes ───────────────────
                    if (value.TryGetProperty("statuses", out var statuses))
                    {
                        foreach (var status in statuses.EnumerateArray())
                        {
                            var id          = status.TryGetProperty("id",         out var sid) ? sid.GetString()    : "?";
                            var statusValue = status.TryGetProperty("status",     out var sv)  ? sv.GetString()     : "?";
                            var recipient   = status.TryGetProperty("recipient_id", out var rid) ? rid.GetString()  : "?";

                            _logger.LogInformation("[WhatsApp] Mensaje {Id} → {Status} (destinatario: {Recipient})",
                                id, statusValue, recipient);
                            // statusValue: sent | delivered | read | failed
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WhatsApp Webhook] Error procesando payload.");
        }

        return Task.CompletedTask;
    }
}

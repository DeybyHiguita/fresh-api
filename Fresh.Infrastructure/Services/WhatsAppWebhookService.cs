using Fresh.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
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

    public async Task ProcessAsync(JsonElement payload, IServiceProvider services)
    {
        try
        {
            _logger.LogInformation("[WhatsApp Webhook] Payload recibido.");

            if (!payload.TryGetProperty("entry", out var entries)) return;

            foreach (var entry in entries.EnumerateArray())
            {
                if (!entry.TryGetProperty("changes", out var changes)) continue;

                foreach (var change in changes.EnumerateArray())
                {
                    var field = change.TryGetProperty("field", out var f) ? f.GetString() : null;
                    if (field != "messages") continue;
                    if (!change.TryGetProperty("value", out var value)) continue;

                    // ── Mensajes entrantes ──────────────────────────────────
                    if (value.TryGetProperty("messages", out var messages))
                    {
                        var contactName = string.Empty;
                        if (value.TryGetProperty("contacts", out var contacts) && contacts.GetArrayLength() > 0)
                            contactName = contacts[0].TryGetProperty("profile", out var profile) &&
                                          profile.TryGetProperty("name", out var nameP)
                                ? nameP.GetString() ?? string.Empty
                                : string.Empty;

                        foreach (var msg in messages.EnumerateArray())
                        {
                            var from    = msg.TryGetProperty("from", out var fr)  ? fr.GetString()  ?? "" : "";
                            var msgType = msg.TryGetProperty("type", out var t)   ? t.GetString()   ?? "" : "";
                            var waMsgId = msg.TryGetProperty("id",   out var mid) ? mid.GetString() ?? "" : "";

                            if (msgType != "text") continue;
                            if (!msg.TryGetProperty("text", out var textObj)) continue;
                            var body = textObj.TryGetProperty("body", out var b) ? b.GetString() ?? "" : "";

                            using var scope  = services.CreateScope();
                            var chatService  = scope.ServiceProvider.GetRequiredService<WhatsappChatService>();
                            var notifier     = scope.ServiceProvider.GetRequiredService<IWhatsappHubNotifier>();

                            var (contact, message) = await chatService.SaveIncomingAsync(
                                from, contactName, body, waMsgId);

                            if (message is not null)
                            {
                                await notifier.NotifyNewMessageAsync(new
                                {
                                    contactId   = contact.Id,
                                    waId        = contact.WaId,
                                    name        = string.IsNullOrWhiteSpace(contact.Name) ? contact.WaId : contact.Name,
                                    unreadCount = contact.UnreadCount,
                                    messageId   = message.Id,
                                    body        = message.Body,
                                    createdAt   = message.CreatedAt.ToString("o"),
                                });
                            }

                            _logger.LogInformation("[WhatsApp] Mensaje de {From}: {Body}", from, body);
                        }
                    }

                    // ── Estados de mensajes salientes ───────────────────────
                    if (value.TryGetProperty("statuses", out var statuses))
                    {
                        foreach (var status in statuses.EnumerateArray())
                        {
                            var waMsgId     = status.TryGetProperty("id",     out var sid) ? sid.GetString() ?? "" : "";
                            var statusValue = status.TryGetProperty("status", out var sv)  ? sv.GetString()  ?? "" : "";

                            if (string.IsNullOrWhiteSpace(waMsgId)) continue;

                            using var scope = services.CreateScope();
                            var chatService = scope.ServiceProvider.GetRequiredService<WhatsappChatService>();
                            await chatService.UpdateMessageStatusAsync(waMsgId, statusValue);

                            _logger.LogInformation("[WhatsApp] Estado {Id} → {Status}", waMsgId, statusValue);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WhatsApp Webhook] Error procesando payload.");
        }
    }
}

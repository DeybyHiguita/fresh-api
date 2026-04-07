using Fresh.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Fresh.Infrastructure.Services;

/// <summary>
/// Procesa los payloads de webhook de Meta WhatsApp Business.
/// Corre en background (fire-and-forget) para devolver 200 OK a Meta de inmediato.
/// </summary>
public class WhatsAppWebhookService
{
    private readonly ILogger<WhatsAppWebhookService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;

    public WhatsAppWebhookService(
        ILogger<WhatsAppWebhookService> logger,
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory)
    {
        _logger           = logger;
        _scopeFactory     = scopeFactory;
        _httpClientFactory = httpClientFactory;
    }

    public async Task ProcessAsync(JsonElement payload)
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

                            string body      = "";
                            string? mediaType = null;
                            string? mediaId   = null;
                            string? mediaName = null;

                            switch (msgType)
                            {
                                case "text":
                                    if (!msg.TryGetProperty("text", out var textObj)) continue;
                                    body = textObj.TryGetProperty("body", out var b) ? b.GetString() ?? "" : "";
                                    break;

                                case "image":
                                    if (msg.TryGetProperty("image", out var imgObj))
                                    {
                                        mediaType = "image";
                                        mediaId   = imgObj.TryGetProperty("id", out var iid) ? iid.GetString() : null;
                                        body      = imgObj.TryGetProperty("caption", out var cap) ? cap.GetString() ?? "" : "";
                                    }
                                    break;

                                case "document":
                                    if (msg.TryGetProperty("document", out var docObj))
                                    {
                                        mediaType = "document";
                                        mediaId   = docObj.TryGetProperty("id",       out var did)  ? did.GetString()       : null;
                                        mediaName = docObj.TryGetProperty("filename", out var fname) ? fname.GetString()     : null;
                                        body      = docObj.TryGetProperty("caption",  out var dcap)  ? dcap.GetString() ?? "" : "";
                                    }
                                    break;

                                case "audio":
                                case "voice":
                                    if (msg.TryGetProperty(msgType, out var audObj))
                                    {
                                        mediaType = "audio";
                                        mediaId   = audObj.TryGetProperty("id", out var aid) ? aid.GetString() : null;
                                        body      = "🎵 Audio";
                                    }
                                    break;

                                case "video":
                                    if (msg.TryGetProperty("video", out var vidObj))
                                    {
                                        mediaType = "video";
                                        mediaId   = vidObj.TryGetProperty("id",      out var vid)  ? vid.GetString()       : null;
                                        body      = vidObj.TryGetProperty("caption", out var vcap) ? vcap.GetString() ?? "" : "🎥 Video";
                                    }
                                    break;

                                default:
                                    _logger.LogInformation("[WhatsApp] Tipo de mensaje no soportado: {Type}", msgType);
                                    continue;
                            }

                            using var scope  = _scopeFactory.CreateScope();
                            var chatService  = scope.ServiceProvider.GetRequiredService<WhatsappChatService>();
                            var notifier     = scope.ServiceProvider.GetRequiredService<IWhatsappHubNotifier>();
                            var appSettings  = scope.ServiceProvider.GetRequiredService<IAppSettingsService>();

                            var (contact, message) = await chatService.SaveIncomingAsync(
                                from, contactName, body, waMsgId, mediaType, mediaId, mediaName);

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
                                    mediaType   = message.MediaType,
                                    mediaId     = message.MediaId,
                                    mediaName   = message.MediaName,
                                    createdAt   = message.CreatedAt.ToString("o"),
                                });
                            }

                            _logger.LogInformation("[WhatsApp] Mensaje {Type} de {From}", msgType, from);
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

                            using var scope = _scopeFactory.CreateScope();
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

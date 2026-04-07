using Fresh.Core.DTOs.WhatsappChat;
using Fresh.Core.Entities;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace Fresh.Infrastructure.Services;

public class WhatsappChatService
{
    private readonly FreshDbContext _db;
    private readonly IAppSettingsService _appSettings;
    private readonly IHttpClientFactory _httpClientFactory;

    public WhatsappChatService(
        FreshDbContext db,
        IAppSettingsService appSettings,
        IHttpClientFactory httpClientFactory)
    {
        _db              = db;
        _appSettings     = appSettings;
        _httpClientFactory = httpClientFactory;
    }

    // ── Contactos ─────────────────────────────────────────────────────────

    public async Task<List<WhatsappContactDto>> GetContactsAsync()
    {
        var contacts = await _db.WhatsappContacts
            .OrderByDescending(c => c.LastMessageAt)
            .ToListAsync();

        var result = new List<WhatsappContactDto>();
        foreach (var c in contacts)
        {
            var last = await _db.WhatsappMessages
                .Where(m => m.ContactId == c.Id)
                .OrderByDescending(m => m.CreatedAt)
                .Select(m => m.Body)
                .FirstOrDefaultAsync() ?? "";

            result.Add(new WhatsappContactDto(
                c.Id,
                c.WaId,
                string.IsNullOrWhiteSpace(c.Name) ? c.WaId : c.Name,
                c.LastMessageAt.ToString("o"),
                c.UnreadCount,
                last.Length > 60 ? last[..60] + "…" : last
            ));
        }
        return result;
    }

    // ── Mensajes de un contacto ───────────────────────────────────────────

    public async Task<List<WhatsappMessageDto>> GetMessagesAsync(int contactId)
    {
        // Marcar como leídos
        await _db.WhatsappContacts
            .Where(c => c.Id == contactId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.UnreadCount, 0));

        var rows = await _db.WhatsappMessages
            .Where(m => m.ContactId == contactId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        return rows.Select(m => new WhatsappMessageDto(
            m.Id,
            m.Direction,
            m.Body,
            m.Status,
            m.CreatedAt.ToString("o"),
            m.MediaType,
            m.MediaId,
            m.MediaName,
            m.WaMessageId,
            m.ReplyToWaMessageId,
            null
        )).ToList();
    }

    // ── Enviar respuesta ──────────────────────────────────────────────────

    public async Task<WhatsappMessageDto> SendReplyAsync(SendMessageRequest req)
    {
        var contact = await _db.WhatsappContacts.FindAsync(req.ContactId)
            ?? throw new KeyNotFoundException($"Contacto {req.ContactId} no encontrado.");

        var settings = await _appSettings.GetAsync();
        if (string.IsNullOrWhiteSpace(settings.WhatsappAccessToken) ||
            string.IsNullOrWhiteSpace(settings.WhatsappPhoneNumberId))
            throw new InvalidOperationException("WhatsApp no está configurado correctamente.");

        // Enviar via Meta API
        string? waMessageId = null;
        var url = $"https://graph.facebook.com/v25.0/{settings.WhatsappPhoneNumberId.Trim()}/messages";

        object payload;
        if (!string.IsNullOrWhiteSpace(req.ReplyToWaMessageId))
        {
            payload = new
            {
                messaging_product = "whatsapp",
                recipient_type    = "individual",
                to                = contact.WaId,
                context           = new { message_id = req.ReplyToWaMessageId },
                type              = "text",
                text              = new { preview_url = false, body = req.Body }
            };
        }
        else
        {
            payload = new
            {
                messaging_product = "whatsapp",
                to                = contact.WaId,
                type              = "text",
                text              = new { preview_url = false, body = req.Body }
            };
        }

        var json    = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        var client  = _httpClientFactory.CreateClient("WhatsApp");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", settings.WhatsappAccessToken.Trim());

        try
        {
            var response = await client.PostAsync(url, content);
            if (response.IsSuccessStatusCode)
            {
                var body     = await response.Content.ReadAsStringAsync();
                var parsed   = JsonSerializer.Deserialize<JsonElement>(body);
                if (parsed.TryGetProperty("messages", out var msgs) &&
                    msgs.GetArrayLength() > 0 &&
                    msgs[0].TryGetProperty("id", out var idProp))
                {
                    waMessageId = idProp.GetString();
                }
            }
        }
        catch { /* loguear en producción */ }

        // Guardar en BD
        var msg = new WhatsappMessage
        {
            ContactId   = contact.Id,
            Direction          = "out",
            Body               = req.Body,
            WaMessageId        = waMessageId,
            ReplyToWaMessageId = req.ReplyToWaMessageId,
            Status             = "sent",
            CreatedAt          = DateTime.UtcNow,
        };
        _db.WhatsappMessages.Add(msg);
        contact.LastMessageAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return new WhatsappMessageDto(msg.Id, "out", msg.Body, msg.Status, msg.CreatedAt.ToString("o"),
            WaMessageId: msg.WaMessageId,
            ReplyToWaMessageId: msg.ReplyToWaMessageId);
    }

    // ── Upsert contacto + guardar mensaje entrante (llamado desde webhook) ─

    public async Task<(WhatsappContact Contact, WhatsappMessage? Message, bool IsNewContact)> SaveIncomingAsync(
        string waId, string contactName, string body, string waMessageId,
        string? mediaType = null, string? mediaId = null, string? mediaName = null)
    {
        // Upsert contacto
        var contact      = await _db.WhatsappContacts.FirstOrDefaultAsync(c => c.WaId == waId);
        bool isNewContact = contact is null;
        if (contact is null)
        {
            contact = new WhatsappContact { WaId = waId, Name = contactName };
            _db.WhatsappContacts.Add(contact);
            await _db.SaveChangesAsync();
        }
        else if (!string.IsNullOrWhiteSpace(contactName) && contact.Name != contactName)
        {
            contact.Name = contactName;
        }

        contact.LastMessageAt = DateTime.UtcNow;
        contact.UnreadCount++;

        // Evitar duplicados por waMessageId
        var exists = await _db.WhatsappMessages.AnyAsync(m => m.WaMessageId == waMessageId);
        if (exists) return (contact, null, false);

        var msg = new WhatsappMessage
        {
            ContactId   = contact.Id,
            Direction   = "in",
            Body        = body,
            WaMessageId = waMessageId,
            Status      = "read",
            MediaType   = mediaType,
            MediaId     = mediaId,
            MediaName   = mediaName,
            CreatedAt   = DateTime.UtcNow,
        };
        _db.WhatsappMessages.Add(msg);
        await _db.SaveChangesAsync();

        return (contact, msg, isNewContact);
    }

    // ── Actualizar estado del mensaje saliente (desde webhook statuses) ───

    public async Task UpdateMessageStatusAsync(string waMessageId, string status)
    {
        await _db.WhatsappMessages
            .Where(m => m.WaMessageId == waMessageId)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.Status, status));
    }

    // ── Marcar mensaje como leído + mostrar indicador de escritura ────────

    public async Task MarkReadWithTypingAsync(string waMessageId)
    {
        try
        {
            var settings = await _appSettings.GetAsync();
            if (string.IsNullOrWhiteSpace(settings.WhatsappAccessToken) ||
                string.IsNullOrWhiteSpace(settings.WhatsappPhoneNumberId))
                return;

            var url = $"https://graph.facebook.com/v25.0/{settings.WhatsappPhoneNumberId.Trim()}/messages";
            var payload = new
            {
                messaging_product = "whatsapp",
                status            = "read",
                message_id        = waMessageId,
                typing_indicator  = new { type = "text" }
            };

            var json    = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var client  = _httpClientFactory.CreateClient("WhatsApp");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", settings.WhatsappAccessToken.Trim());

            await client.PostAsync(url, content);
        }
        catch { /* no bloquear el flujo si falla */ }
    }

    // ── Marcar mensajes leídos desde la UI (el admin abre la conversación) ─

    public async Task MarkContactMessagesReadAsync(int contactId)
    {
        var contact = await _db.WhatsappContacts.FindAsync(contactId)
            ?? throw new KeyNotFoundException($"Contacto {contactId} no encontrado.");

        // Obtener el último mensaje entrante no leído
        var lastIncoming = await _db.WhatsappMessages
            .Where(m => m.ContactId == contactId && m.Direction == "in" && !string.IsNullOrEmpty(m.WaMessageId))
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync();

        // Marcar unread = 0 en BD
        await _db.WhatsappContacts
            .Where(c => c.Id == contactId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.UnreadCount, 0));

        // Enviar mark-as-read a Meta (sin typing indicator — el admin solo está leyendo)
        if (lastIncoming?.WaMessageId is not null)
        {
            try
            {
                var settings = await _appSettings.GetAsync();
                if (string.IsNullOrWhiteSpace(settings.WhatsappAccessToken) ||
                    string.IsNullOrWhiteSpace(settings. WhatsappPhoneNumberId))
                    return;

                var url = $"https://graph.facebook.com/v25.0/{settings.WhatsappPhoneNumberId.Trim()}/messages";
                var payload = new
                {
                    messaging_product = "whatsapp",
                    status            = "read",
                    message_id        = lastIncoming.WaMessageId
                };

                var json    = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var client  = _httpClientFactory.CreateClient("WhatsApp");
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", settings.WhatsappAccessToken.Trim());

                await client.PostAsync(url, content);
            }
            catch { /* no bloquear */ }
        }
    }

    // ── Obtener WaId de un contacto por su ID interno ─────────────────────

    public async Task<string> GetContactWaIdAsync(int contactId)
    {
        var contact = await _db.WhatsappContacts.FindAsync(contactId)
            ?? throw new KeyNotFoundException($"Contacto {contactId} no encontrado.");
        return contact.WaId;
    }

    // ── Obtener URL de descarga de un media_id de Meta ────────────────────
    // Meta devuelve una URL temporal (~5 min). El frontend la usa directamente.

    public async Task<string?> GetMediaUrlAsync(string mediaId)
    {
        var settings = await _appSettings.GetAsync();
        if (string.IsNullOrWhiteSpace(settings.WhatsappAccessToken)) return null;

        var client = _httpClientFactory.CreateClient("WhatsApp");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", settings.WhatsappAccessToken.Trim());

        try
        {
            var res = await client.GetAsync($"https://graph.facebook.com/v25.0/{mediaId}");
            if (!res.IsSuccessStatusCode) return null;
            var json   = await res.Content.ReadAsStringAsync();
            var parsed = JsonSerializer.Deserialize<JsonElement>(json);
            return parsed.TryGetProperty("url", out var u) ? u.GetString() : null;
        }
        catch { return null; }
    }

    // ── Descargar y proxy del archivo media (evita exponer token al frontend) ─

    public async Task<(byte[]? Data, string? MimeType)> DownloadMediaAsync(string mediaId)
    {
        var settings = await _appSettings.GetAsync();
        if (string.IsNullOrWhiteSpace(settings.WhatsappAccessToken)) return (null, null);

        var client = _httpClientFactory.CreateClient("WhatsApp");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", settings.WhatsappAccessToken.Trim());

        try
        {
            // 1. Obtener URL del media
            var metaRes = await client.GetAsync($"https://graph.facebook.com/v25.0/{mediaId}");
            if (!metaRes.IsSuccessStatusCode) return (null, null);
            var metaJson = await metaRes.Content.ReadAsStringAsync();
            var metaParsed = JsonSerializer.Deserialize<JsonElement>(metaJson);
            if (!metaParsed.TryGetProperty("url", out var urlProp)) return (null, null);
            var mediaUrl  = urlProp.GetString()!;
            var mimeType  = metaParsed.TryGetProperty("mime_type", out var mt) ? mt.GetString() : "application/octet-stream";

            // 2. Descargar el archivo usando el mismo token
            var fileRes = await client.GetAsync(mediaUrl);
            if (!fileRes.IsSuccessStatusCode) return (null, null);
            var data = await fileRes.Content.ReadAsByteArrayAsync();
            return (data, mimeType);
        }
        catch { return (null, null); }
    }

    // ── Enviar media (imagen / documento) ────────────────────────────────

    public async Task<WhatsappMessageDto> SendMediaAsync(int contactId, IFormFile file)
    {
        var contact = await _db.WhatsappContacts.FindAsync(contactId)
            ?? throw new KeyNotFoundException($"Contacto {contactId} no encontrado.");

        var settings = await _appSettings.GetAsync();
        if (string.IsNullOrWhiteSpace(settings.WhatsappAccessToken) ||
            string.IsNullOrWhiteSpace(settings.WhatsappPhoneNumberId))
            throw new InvalidOperationException("WhatsApp no está configurado correctamente.");

        var token      = settings.WhatsappAccessToken.Trim();
        var phoneNumId = settings.WhatsappPhoneNumberId.Trim();

        // 1. Subir el archivo a Meta
        var client = _httpClientFactory.CreateClient("WhatsApp");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        using var fileStream = file.OpenReadStream();
        using var multipart  = new MultipartFormDataContent();
        var fileContent      = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
            file.ContentType ?? "application/octet-stream");
        multipart.Add(fileContent, "file", file.FileName);
        multipart.Add(new StringContent("whatsapp"), "messaging_product");

        var uploadRes = await client.PostAsync(
            $"https://graph.facebook.com/v25.0/{phoneNumId}/media", multipart);

        if (!uploadRes.IsSuccessStatusCode)
        {
            var err = await uploadRes.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Error subiendo media a Meta: {err}");
        }

        var uploadJson = await uploadRes.Content.ReadAsStringAsync();
        var uploadParsed = JsonSerializer.Deserialize<JsonElement>(uploadJson);
        if (!uploadParsed.TryGetProperty("id", out var mediaIdProp))
            throw new InvalidOperationException("Meta no devolvió media_id.");

        var waMediaId = mediaIdProp.GetString()!;

        // 2. Detectar tipo de media
        var contentType = file.ContentType ?? "";
        var mediaType   = (contentType == "image/webp" || file.FileName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
                            ? "sticker"
                        : contentType.StartsWith("image/")  ? "image"
                        : contentType.StartsWith("video/")  ? "video"
                        : contentType.StartsWith("audio/")  ? "audio"
                        : "document";

        // 3. Enviar mensaje con el media_id
        object mediaPayloadBody = mediaType switch
        {
            "sticker"  => new { id = waMediaId },
            "image"    => new { id = waMediaId, caption = "" },
            "video"    => new { id = waMediaId, caption = "" },
            "audio"    => new { id = waMediaId },
            _          => new { id = waMediaId, filename = file.FileName },
        };

        var sendPayload = new Dictionary<string, object>
        {
            ["messaging_product"] = "whatsapp",
            ["to"]                = contact.WaId,
            ["type"]              = mediaType,
            [mediaType]           = mediaPayloadBody,
        };

        var sendJson    = JsonSerializer.Serialize(sendPayload);
        var sendContent = new StringContent(sendJson, System.Text.Encoding.UTF8, "application/json");
        var sendRes     = await client.PostAsync(
            $"https://graph.facebook.com/v25.0/{phoneNumId}/messages", sendContent);

        string? waMessageId = null;
        if (sendRes.IsSuccessStatusCode)
        {
            var sendBody   = await sendRes.Content.ReadAsStringAsync();
            var sendParsed = JsonSerializer.Deserialize<JsonElement>(sendBody);
            if (sendParsed.TryGetProperty("messages", out var msgs) && msgs.GetArrayLength() > 0)
                waMessageId = msgs[0].TryGetProperty("id", out var wid) ? wid.GetString() : null;
        }

        // 4. Guardar en BD
        var msg = new WhatsappMessage
        {
            ContactId   = contact.Id,
            Direction   = "out",
            Body        = file.FileName,
            WaMessageId = waMessageId,
            Status      = "sent",
            MediaType   = mediaType,
            MediaId     = waMediaId,
            MediaName   = file.FileName,
            CreatedAt   = DateTime.UtcNow,
        };
        _db.WhatsappMessages.Add(msg);
        contact.LastMessageAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return new WhatsappMessageDto(msg.Id, "out", msg.Body, msg.Status,
            msg.CreatedAt.ToString("o"), msg.MediaType, msg.MediaId, msg.MediaName);
    }

    // ── Enviar prompt interactivo de domicilio ────────────────────────────
    // Envía un mensaje con botón de respuesta rápida para solicitar datos.
    // Requiere que el cliente haya escrito dentro de las últimas 24 h.

    private const string DeliveryButtonId = "SOLICITAR_DOMICILIO";

    public async Task<WhatsappMessageDto> SendDeliveryPromptAsync(int contactId)
    {
        var contact = await _db.WhatsappContacts.FindAsync(contactId)
            ?? throw new KeyNotFoundException($"Contacto {contactId} no encontrado.");

        var settings = await _appSettings.GetAsync();
        if (string.IsNullOrWhiteSpace(settings.WhatsappAccessToken) ||
            string.IsNullOrWhiteSpace(settings.WhatsappPhoneNumberId))
            throw new InvalidOperationException("WhatsApp no está configurado correctamente.");

        var token      = settings.WhatsappAccessToken.Trim();
        var phoneNumId = settings.WhatsappPhoneNumberId.Trim();

        var client = _httpClientFactory.CreateClient("WhatsApp");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Mensaje interactivo con botón de respuesta rápida
        var payload = new
        {
            messaging_product = "whatsapp",
            to                = contact.WaId,
            type              = "interactive",
            interactive       = new
            {
                type = "button",
                body = new
                {
                    text = "¡Hola! 👋 Para procesar tu domicilio en *Fresh*, presiona el botón de abajo y te enviaré el formato para completar. 🛵"
                },
                action = new
                {
                    buttons = new[]
                    {
                        new
                        {
                            type  = "reply",
                            reply = new { id = DeliveryButtonId, title = "Enviar mis datos 🛵" }
                        }
                    }
                }
            }
        };

        var json    = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        string? waMessageId = null;
        var res = await client.PostAsync(
            $"https://graph.facebook.com/v25.0/{phoneNumId}/messages", content);

        if (res.IsSuccessStatusCode)
        {
            var body   = await res.Content.ReadAsStringAsync();
            var parsed = JsonSerializer.Deserialize<JsonElement>(body);
            if (parsed.TryGetProperty("messages", out var msgs) && msgs.GetArrayLength() > 0)
                waMessageId = msgs[0].TryGetProperty("id", out var wid) ? wid.GetString() : null;
        }

        var bodyText = "🛵 Solicité datos de domicilio";
        var msg = new WhatsappMessage
        {
            ContactId   = contact.Id,
            Direction   = "out",
            Body        = bodyText,
            WaMessageId = waMessageId,
            Status      = "sent",
            CreatedAt   = DateTime.UtcNow,
        };
        _db.WhatsappMessages.Add(msg);
        contact.LastMessageAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return new WhatsappMessageDto(msg.Id, "out", bodyText, "sent", msg.CreatedAt.ToString("o"));
    }

    // ── Auto-respuesta cuando el cliente presiona el botón ─────────────────
    // Envía el formato de datos directamente al cliente (llamado desde webhook).

    public async Task SendDeliveryFormatAsync(string waId)
    {
        var settings = await _appSettings.GetAsync();
        if (string.IsNullOrWhiteSpace(settings.WhatsappAccessToken) ||
            string.IsNullOrWhiteSpace(settings.WhatsappPhoneNumberId))
            return;

        var token      = settings.WhatsappAccessToken.Trim();
        var phoneNumId = settings.WhatsappPhoneNumberId.Trim();

        var client = _httpClientFactory.CreateClient("WhatsApp");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        const string formato =
            "📋 *Perfecto, completa estos datos y envíalos:*\n\n" +
            "🏠 *Dirección:* \n" +
            "🏘️ *Barrio:* \n" +
            "👤 *Nombre:* \n" +
            "📞 *Teléfono:* \n" +
            "🛒 *Pedido:* \n" +
            "💰 *Pago:* Efectivo / Nequi / Transferencia";

        var payload = new
        {
            messaging_product = "whatsapp",
            to                = waId,
            type              = "text",
            text              = new { preview_url = false, body = formato }
        };

        var json    = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        await client.PostAsync($"https://graph.facebook.com/v25.0/{phoneNumId}/messages", content);

        // Guardar el mensaje enviado en BD
        var contact = await _db.WhatsappContacts.FirstOrDefaultAsync(c => c.WaId == waId);
        if (contact is not null)
        {
            _db.WhatsappMessages.Add(new WhatsappMessage
            {
                ContactId = contact.Id,
                Direction = "out",
                Body      = formato,
                Status    = "sent",
                CreatedAt = DateTime.UtcNow,
            });
            contact.LastMessageAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public static string GetDeliveryButtonId()    => DeliveryButtonId;
    public static string GetMenuOptionHablar()    => "HABLAR_TIENDA";
    public static string GetMenuOptionVerMenu()   => "VER_MENU";
    public static string GetMenuOptionDomicilio() => "HACER_DOMICILIO";

    // ── Menú de bienvenida (lista interactiva con 3 opciones) ─────────────

    public async Task SendWelcomeMenuAsync(string waId)
    {
        var settings = await _appSettings.GetAsync();
        if (string.IsNullOrWhiteSpace(settings.WhatsappAccessToken) ||
            string.IsNullOrWhiteSpace(settings.WhatsappPhoneNumberId))
            return;

        var client = _httpClientFactory.CreateClient("WhatsApp");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", settings.WhatsappAccessToken.Trim());

        var payload = new
        {
            messaging_product = "whatsapp",
            to                = waId,
            type              = "interactive",
            interactive       = new
            {
                type   = "list",
                header = new { type = "text", text = "¡Bienvenid@ a Fresh! 🌿" },
                body   = new { text = "¿en qué podemos ayudarte hoy? Selecciona una opción:" },
                footer = new { text = "Fresh · Tu tienda de confianza" },
                action = new
                {
                    button   = "Ver opciones",
                    sections = new[]
                    {
                        new
                        {
                            rows = new object[]
                            {
                                new { id = GetMenuOptionHablar(),    title = "Hablar con la tienda 💬", description = "Chatea con un agente" },
                                new { id = GetMenuOptionVerMenu(),   title = "Ver menú 🍽️",             description = "Conoce nuestros productos" },
                                new { id = GetMenuOptionDomicilio(), title = "Hacer domicilio 🛵",       description = "Pide a domicilio" },
                            }
                        }
                    }
                }
            }
        };

        var json    = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var res = await client.PostAsync(
            $"https://graph.facebook.com/v25.0/{settings.WhatsappPhoneNumberId.Trim()}/messages", content);

        if (res.IsSuccessStatusCode)
        {
            var contact = await _db.WhatsappContacts.FirstOrDefaultAsync(c => c.WaId == waId);
            if (contact is not null)
            {
                _db.WhatsappMessages.Add(new WhatsappMessage
                {
                    ContactId = contact.Id,
                    Direction = "out",
                    Body      = "📋 Menú de bienvenida enviado",
                    Status    = "sent",
                    CreatedAt = DateTime.UtcNow,
                });
                contact.LastMessageAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }
    }

    // ── Enviar botón de URL del menú al cliente ───────────────────────────

    public async Task SendMenuUrlAsync(string waId)
    {
        var settings = await _appSettings.GetAsync();
        if (string.IsNullOrWhiteSpace(settings.WhatsappAccessToken) ||
            string.IsNullOrWhiteSpace(settings.WhatsappPhoneNumberId))
            return;

        var client = _httpClientFactory.CreateClient("WhatsApp");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", settings.WhatsappAccessToken.Trim());

        var payload = new
        {
            messaging_product = "whatsapp",
            to                = waId,
            type              = "interactive",
            interactive       = new
            {
                type   = "cta_url",
                body   = new { text = "¡Aquí está nuestro menú completo! 🍽️ Toca el botón para verlo." },
                action = new
                {
                    name       = "cta_url",
                    parameters = new
                    {
                        display_text = "Ver menú 🍽️",
                        url          = "https://fresh-app-production.up.railway.app/menu"
                    }
                }
            }
        };

        var json    = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        await client.PostAsync(
            $"https://graph.facebook.com/v25.0/{settings.WhatsappPhoneNumberId.Trim()}/messages", content);

        var contact = await _db.WhatsappContacts.FirstOrDefaultAsync(c => c.WaId == waId);
        if (contact is not null)
        {
            _db.WhatsappMessages.Add(new WhatsappMessage
            {
                ContactId = contact.Id,
                Direction = "out",
                Body      = "🍽️ Menú de productos enviado",
                Status    = "sent",
                CreatedAt = DateTime.UtcNow,
            });
            contact.LastMessageAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }
}

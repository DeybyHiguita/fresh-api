using Fresh.Core.DTOs.WhatsappChat;
using Fresh.Core.Entities;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
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

        return await _db.WhatsappMessages
            .Where(m => m.ContactId == contactId)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new WhatsappMessageDto(
                m.Id,
                m.Direction,
                m.Body,
                m.Status,
                m.CreatedAt.ToString("o")
            ))
            .ToListAsync();
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
        var url = $"https://graph.facebook.com/v19.0/{settings.WhatsappPhoneNumberId.Trim()}/messages";

        var payload = new
        {
            messaging_product = "whatsapp",
            to   = contact.WaId,
            type = "text",
            text = new { preview_url = false, body = req.Body }
        };

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
            Direction   = "out",
            Body        = req.Body,
            WaMessageId = waMessageId,
            Status      = "sent",
            CreatedAt   = DateTime.UtcNow,
        };
        _db.WhatsappMessages.Add(msg);
        contact.LastMessageAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return new WhatsappMessageDto(msg.Id, "out", msg.Body, msg.Status, msg.CreatedAt.ToString("o"));
    }

    // ── Upsert contacto + guardar mensaje entrante (llamado desde webhook) ─

    public async Task<(WhatsappContact Contact, WhatsappMessage Message)> SaveIncomingAsync(
        string waId, string contactName, string body, string waMessageId)
    {
        // Upsert contacto
        var contact = await _db.WhatsappContacts.FirstOrDefaultAsync(c => c.WaId == waId);
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
        if (exists) return (contact, null!);

        var msg = new WhatsappMessage
        {
            ContactId   = contact.Id,
            Direction   = "in",
            Body        = body,
            WaMessageId = waMessageId,
            Status      = "read",
            CreatedAt   = DateTime.UtcNow,
        };
        _db.WhatsappMessages.Add(msg);
        await _db.SaveChangesAsync();

        return (contact, msg);
    }

    // ── Actualizar estado del mensaje saliente (desde webhook statuses) ───

    public async Task UpdateMessageStatusAsync(string waMessageId, string status)
    {
        await _db.WhatsappMessages
            .Where(m => m.WaMessageId == waMessageId)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.Status, status));
    }
}

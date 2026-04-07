using Fresh.Core.DTOs.Order;
using Fresh.Core.Interfaces;
using System.Text;
using System.Text.Json;

namespace Fresh.Infrastructure.Services;

/// <summary>
/// Envía mensajes de WhatsApp vía Meta Cloud API usando plantillas de texto libre (within 24h window)
/// o mensajes de plantilla aprobados.
/// Documentación: https://developers.facebook.com/docs/whatsapp/cloud-api/messages
/// </summary>
public class WhatsAppNotificationService
{
    private readonly IAppSettingsService _appSettings;
    private readonly IHttpClientFactory _httpClientFactory;

    public WhatsAppNotificationService(
        IAppSettingsService appSettings,
        IHttpClientFactory httpClientFactory)
    {
        _appSettings       = appSettings;
        _httpClientFactory = httpClientFactory;
    }

    public async Task NotifyNewOrderAsync(OrderResponse order)
    {
        var settings = await _appSettings.GetAsync();
        if (!settings.WhatsappNotificationsEnabled) return;
        if (!IsConfigured(settings)) return;

        var customer = !string.IsNullOrWhiteSpace(order.CustomerName)
            ? order.CustomerName : order.UserName;
        var notes    = !string.IsNullOrWhiteSpace(order.Notes) ? order.Notes : "Sin notas";

        // Plantilla aprobada: nueva_orden_admin
        // {{1}} = # orden  {{2}} = cliente  {{3}} = tipo  {{4}} = pago  {{5}} = total  {{6}} = estado  {{7}} = notas
        await SendTemplateAsync(settings, "nueva_orden_admin", "es",
            order.Id.ToString(),
            customer,
            order.OrderType,
            order.PaymentMethod,
            $"${order.Total:N0}",
            order.Status,
            notes);
    }

    public async Task NotifyStatusChangedAsync(OrderResponse order)
    {
        var settings = await _appSettings.GetAsync();
        if (!settings.WhatsappNotificationsEnabled) return;
        if (!IsConfigured(settings)) return;

        var iconMap = new Dictionary<string, string>
        {
            { "Pendiente",       "🕐" },
            { "En preparación",  "🍳" },
            { "Listo",           "✅" },
            { "Entregado",       "📦" },
            { "Cancelado",       "❌" },
        };
        var icon = iconMap.GetValueOrDefault(order.Status, "🔄");
        var customer = !string.IsNullOrWhiteSpace(order.CustomerName)
            ? order.CustomerName : order.UserName;

        var text =
            $"{icon} *Orden #{order.Id} actualizada*\n" +
            $"👤 {customer}\n" +
            $"📋 Nuevo estado: *{order.Status}*";

        await SendTextAsync(settings, text);
    }

    // ── Core send ─────────────────────────────────────────────────────────

    private async Task SendTemplateAsync(
        Fresh.Core.DTOs.AppSettings.AppSettingsResponse settings,
        string templateName,
        string languageCode,
        params string[] bodyParams)
    {
        var phone      = settings.WhatsappAdminPhone.Trim();
        var token      = settings.WhatsappAccessToken.Trim();
        var phoneNumId = settings.WhatsappPhoneNumberId.Trim();

        var url = $"https://graph.facebook.com/v19.0/{phoneNumId}/messages";

        var parameters = bodyParams
            .Select(p => new { type = "text", text = p })
            .ToArray<object>();

        var payload = new
        {
            messaging_product = "whatsapp",
            to   = phone,
            type = "template",
            template = new
            {
                name     = templateName,
                language = new { code = languageCode },
                components = new[]
                {
                    new
                    {
                        type       = "body",
                        parameters = parameters
                    }
                }
            }
        };

        var json    = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var client = _httpClientFactory.CreateClient("WhatsApp");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        try
        {
            var response = await client.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[WhatsApp] Template error {response.StatusCode}: {body}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WhatsApp] Excepción al enviar plantilla: {ex.Message}");
        }
    }

    private async Task SendTextAsync(
        Fresh.Core.DTOs.AppSettings.AppSettingsResponse settings,
        string text)
    {
        var phone       = settings.WhatsappAdminPhone.Trim();
        var token       = settings.WhatsappAccessToken.Trim();
        var phoneNumId  = settings.WhatsappPhoneNumberId.Trim();

        var url = $"https://graph.facebook.com/v19.0/{phoneNumId}/messages";

        var payload = new
        {
            messaging_product = "whatsapp",
            to                = phone,
            type              = "text",
            text              = new { preview_url = false, body = text }
        };

        var json    = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var client = _httpClientFactory.CreateClient("WhatsApp");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        try
        {
            var response = await client.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[WhatsApp] Error {response.StatusCode}: {body}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WhatsApp] Excepción al enviar: {ex.Message}");
        }
    }

    private static bool IsConfigured(Fresh.Core.DTOs.AppSettings.AppSettingsResponse s) =>
        !string.IsNullOrWhiteSpace(s.WhatsappAdminPhone) &&
        !string.IsNullOrWhiteSpace(s.WhatsappAccessToken) &&
        !string.IsNullOrWhiteSpace(s.WhatsappPhoneNumberId);

    // ── Test ──────────────────────────────────────────────────────────────

    public async Task<(bool Success, string Error)> SendHelloWorldAsync()
    {
        var settings = await _appSettings.GetAsync();
        if (!IsConfigured(settings))
            return (false, "Faltan datos de configuración (teléfono, token o Phone Number ID).");

        var phone      = settings.WhatsappAdminPhone.Trim();
        var token      = settings.WhatsappAccessToken.Trim();
        var phoneNumId = settings.WhatsappPhoneNumberId.Trim();

        var url = $"https://graph.facebook.com/v19.0/{phoneNumId}/messages";

        var payload = new
        {
            messaging_product = "whatsapp",
            to   = phone,
            type = "template",
            template = new
            {
                name     = "hello_world",
                language = new { code = "en_US" }
            }
        };

        var json    = System.Text.Json.JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var client = _httpClientFactory.CreateClient("WhatsApp");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        try
        {
            var response = await client.PostAsync(url, content);
            var body     = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
                return (true, string.Empty);

            Console.WriteLine($"[WhatsApp] Test error {response.StatusCode}: {body}");
            return (false, $"Error {(int)response.StatusCode}: {body}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WhatsApp] Test excepción: {ex.Message}");
            return (false, ex.Message);
        }
    }
}

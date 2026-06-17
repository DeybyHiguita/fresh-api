using Fresh.Core.DTOs.AppSettings;
using Fresh.Core.Entities;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Fresh.Infrastructure.Services;

public class AppSettingsService : IAppSettingsService
{
    private const string KeyWhatsapp       = "whatsapp_notifications_enabled";
    private const string KeyNotifyCreate   = "whatsapp_notify_on_create";
    private const string KeyNotifyUpdate   = "whatsapp_notify_on_update";
    private const string KeyPhone          = "whatsapp_admin_phone";
    private const string KeyToken          = "whatsapp_access_token";
    private const string KeyPhoneNumberId  = "whatsapp_phone_number_id";
    private const string KeyGeminiApiKey   = "gemini_api_key"; // valor cifrado

    private readonly FreshDbContext _context;
    private readonly ISecretProtector _protector;
    private readonly IConfiguration _config;

    public AppSettingsService(FreshDbContext context, ISecretProtector protector, IConfiguration config)
    {
        _context   = context;
        _protector = protector;
        _config    = config;
    }

    public async Task<AppSettingsResponse> GetAsync()
    {
        var settings = await _context.AppSettings.ToListAsync();
        return Map(settings);
    }

    public async Task<string?> GetGeminiApiKeyAsync()
    {
        var stored = await _context.AppSettings
            .Where(s => s.Key == KeyGeminiApiKey)
            .Select(s => s.Value)
            .FirstOrDefaultAsync();

        var key = _protector.Decrypt(stored);
        // Fallback a configuración (appsettings) por retrocompatibilidad
        return string.IsNullOrWhiteSpace(key) ? _config["Gemini:ApiKey"] : key;
    }

    public async Task<AppSettingsResponse> UpdateAsync(UpdateAppSettingsRequest request)
    {
        await Upsert(KeyWhatsapp,
            request.WhatsappNotificationsEnabled ? "true" : "false",
            "Habilitar notificaciones por WhatsApp al administrador");
        await Upsert(KeyNotifyCreate,
            request.WhatsappNotifyOnCreate ? "true" : "false",
            "Notificar al administrador cuando se crea una orden nueva");
        await Upsert(KeyNotifyUpdate,
            request.WhatsappNotifyOnUpdate ? "true" : "false",
            "Notificar al administrador cuando una orden cambia de estado");
        await Upsert(KeyPhone,
            request.WhatsappAdminPhone,
            "Número WhatsApp del administrador (formato internacional, ej: 573001234567)");
        await Upsert(KeyToken,
            request.WhatsappAccessToken,
            "Token de acceso permanente de Meta WhatsApp Business API");
        await Upsert(KeyPhoneNumberId,
            request.WhatsappPhoneNumberId,
            "Phone Number ID del remitente en Meta WhatsApp Business");

        // Gemini API key: solo se actualiza si viene una nueva (se guarda cifrada)
        if (!string.IsNullOrWhiteSpace(request.GeminiApiKey))
        {
            await Upsert(KeyGeminiApiKey,
                _protector.Encrypt(request.GeminiApiKey.Trim()),
                "API key de Google Gemini (cifrada)");
        }

        await _context.SaveChangesAsync();
        return await GetAsync();
    }

    // ── Helpers ──────────────────────────────────────────
    private AppSettingsResponse Map(List<AppSetting> list)
    {
        string Get(string k) => list.FirstOrDefault(s => s.Key == k)?.Value ?? string.Empty;
        // Para flags nuevos: si la clave no existe aún, default = true (retrocompatibilidad)
        bool GetBoolDefaultTrue(string k)
        {
            var v = list.FirstOrDefault(s => s.Key == k)?.Value;
            return v == null || v == "true";
        }
        // Gemini key: descifrar solo para indicar estado y vista enmascarada (no se expone completa)
        var geminiKey = _protector.Decrypt(Get(KeyGeminiApiKey));
        if (string.IsNullOrWhiteSpace(geminiKey))
            geminiKey = _config["Gemini:ApiKey"];

        return new AppSettingsResponse
        {
            WhatsappNotificationsEnabled = Get(KeyWhatsapp) == "true",
            WhatsappNotifyOnCreate       = GetBoolDefaultTrue(KeyNotifyCreate),
            WhatsappNotifyOnUpdate       = GetBoolDefaultTrue(KeyNotifyUpdate),
            WhatsappAdminPhone           = Get(KeyPhone),
            WhatsappAccessToken          = Get(KeyToken),
            WhatsappPhoneNumberId        = Get(KeyPhoneNumberId),
            GeminiApiKeyConfigured       = !string.IsNullOrWhiteSpace(geminiKey),
            GeminiApiKeyMasked           = MaskKey(geminiKey),
        };
    }

    private static string MaskKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return string.Empty;
        var last = key.Length >= 4 ? key[^4..] : key;
        return $"••••••••{last}";
    }

    private async Task Upsert(string key, string value, string description)
    {
        var setting = await _context.AppSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting == null)
        {
            _context.AppSettings.Add(new AppSetting
            {
                Key = key,
                Value = value,
                Description = description,
                UpdatedAt = DateTime.UtcNow,
            });
        }
        else
        {
            setting.Value = value;
            setting.UpdatedAt = DateTime.UtcNow;
        }
    }
}

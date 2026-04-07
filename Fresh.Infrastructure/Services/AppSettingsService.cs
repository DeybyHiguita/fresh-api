using Fresh.Core.DTOs.AppSettings;
using Fresh.Core.Entities;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fresh.Infrastructure.Services;

public class AppSettingsService : IAppSettingsService
{
    private const string KeyWhatsapp       = "whatsapp_notifications_enabled";
    private const string KeyPhone          = "whatsapp_admin_phone";
    private const string KeyToken          = "whatsapp_access_token";
    private const string KeyPhoneNumberId  = "whatsapp_phone_number_id";

    private readonly FreshDbContext _context;

    public AppSettingsService(FreshDbContext context)
    {
        _context = context;
    }

    public async Task<AppSettingsResponse> GetAsync()
    {
        var settings = await _context.AppSettings.ToListAsync();
        return Map(settings);
    }

    public async Task<AppSettingsResponse> UpdateAsync(UpdateAppSettingsRequest request)
    {
        await Upsert(KeyWhatsapp,
            request.WhatsappNotificationsEnabled ? "true" : "false",
            "Enviar notificación por WhatsApp al administrador cuando se crea una orden");
        await Upsert(KeyPhone,
            request.WhatsappAdminPhone,
            "Número WhatsApp del administrador (formato internacional, ej: 573001234567)");
        await Upsert(KeyToken,
            request.WhatsappAccessToken,
            "Token de acceso permanente de Meta WhatsApp Business API");
        await Upsert(KeyPhoneNumberId,
            request.WhatsappPhoneNumberId,
            "Phone Number ID del remitente en Meta WhatsApp Business");

        await _context.SaveChangesAsync();
        return await GetAsync();
    }

    // ── Helpers ──────────────────────────────────────────
    private AppSettingsResponse Map(List<AppSetting> list)
    {
        string Get(string k) => list.FirstOrDefault(s => s.Key == k)?.Value ?? string.Empty;
        return new AppSettingsResponse
        {
            WhatsappNotificationsEnabled = Get(KeyWhatsapp) == "true",
            WhatsappAdminPhone           = Get(KeyPhone),
            WhatsappAccessToken          = Get(KeyToken),
            WhatsappPhoneNumberId        = Get(KeyPhoneNumberId),
        };
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

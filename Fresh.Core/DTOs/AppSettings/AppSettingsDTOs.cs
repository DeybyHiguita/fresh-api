namespace Fresh.Core.DTOs.AppSettings;

public class AppSettingsResponse
{
    public bool   WhatsappNotificationsEnabled { get; set; }
    public bool   WhatsappNotifyOnCreate       { get; set; } = true;
    public bool   WhatsappNotifyOnUpdate       { get; set; } = true;
    public string WhatsappAdminPhone           { get; set; } = string.Empty;
    public string WhatsappAccessToken          { get; set; } = string.Empty;
    public string WhatsappPhoneNumberId        { get; set; } = string.Empty;

    /// <summary>Indica si hay una API key de Gemini configurada (no se expone la clave real).</summary>
    public bool   GeminiApiKeyConfigured       { get; set; }
    /// <summary>Vista enmascarada de la key (ej: "••••••1a2b") solo para referencia visual.</summary>
    public string GeminiApiKeyMasked           { get; set; } = string.Empty;
}

public class UpdateAppSettingsRequest
{
    public bool   WhatsappNotificationsEnabled { get; set; }
    public bool   WhatsappNotifyOnCreate       { get; set; } = true;
    public bool   WhatsappNotifyOnUpdate       { get; set; } = true;
    public string WhatsappAdminPhone           { get; set; } = string.Empty;
    public string WhatsappAccessToken          { get; set; } = string.Empty;
    public string WhatsappPhoneNumberId        { get; set; } = string.Empty;

    /// <summary>Nueva API key de Gemini en texto plano. Si es null/vacía, no se modifica la existente.</summary>
    public string? GeminiApiKey                { get; set; }
}

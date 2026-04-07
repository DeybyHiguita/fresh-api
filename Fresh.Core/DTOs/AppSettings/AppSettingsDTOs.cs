namespace Fresh.Core.DTOs.AppSettings;

public class AppSettingsResponse
{
    public bool   WhatsappNotificationsEnabled { get; set; }
    public string WhatsappAdminPhone           { get; set; } = string.Empty;
    public string WhatsappAccessToken          { get; set; } = string.Empty;
    public string WhatsappPhoneNumberId        { get; set; } = string.Empty;
}

public class UpdateAppSettingsRequest
{
    public bool   WhatsappNotificationsEnabled { get; set; }
    public string WhatsappAdminPhone           { get; set; } = string.Empty;
    public string WhatsappAccessToken          { get; set; } = string.Empty;
    public string WhatsappPhoneNumberId        { get; set; } = string.Empty;
}

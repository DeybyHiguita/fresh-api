using Fresh.Core.DTOs.AppSettings;

namespace Fresh.Core.Interfaces;

public interface IAppSettingsService
{
    Task<AppSettingsResponse> GetAsync();
    Task<AppSettingsResponse> UpdateAsync(UpdateAppSettingsRequest request);

    /// <summary>Devuelve la API key de Gemini descifrada para uso interno del servidor.</summary>
    Task<string?> GetGeminiApiKeyAsync();
}

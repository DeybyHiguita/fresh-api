using Fresh.Core.DTOs.AppSettings;

namespace Fresh.Core.Interfaces;

public interface IAppSettingsService
{
    Task<AppSettingsResponse> GetAsync();
    Task<AppSettingsResponse> UpdateAsync(UpdateAppSettingsRequest request);
}

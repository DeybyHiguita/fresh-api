using Fresh.Core.DTOs.AppPage;

namespace Fresh.Core.Interfaces;

public interface IAppPageService
{
    Task<IEnumerable<AppPageResponse>> GetAllAsync(bool onlyActive = true);
    Task<AppPageResponse?> GetByIdAsync(int id);
    Task<AppPageResponse> CreateAsync(AppPageRequest request);
    Task<AppPageResponse?> UpdateAsync(int id, AppPageRequest request);
    Task<bool> DeleteAsync(int id);
}
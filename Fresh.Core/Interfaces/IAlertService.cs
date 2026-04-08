using Fresh.Core.DTOs.Alert;

namespace Fresh.Core.Interfaces;

public interface IAlertService
{
    Task<IEnumerable<AlertResponse>> GetAllAsync();
    Task<AlertResponse> CreateAsync(AlertRequest request, int userId);
    Task<AlertResponse?> UpdateAsync(int id, AlertRequest request);
    Task<bool> DeleteAsync(int id);
    Task<AlertResponse?> MarkSentAsync(int id);
}

using Fresh.Core.DTOs.Log;

namespace Fresh.Core.Interfaces;

public interface ILogService
{
    Task<PagedLogResponse> GetAllAsync(LogFilterRequest filter);
    Task<LogResponse?> GetByIdAsync(long id);
    Task<LogResponse> CreateAsync(LogRequest request);
    Task<bool> DeleteAsync(long id);
}

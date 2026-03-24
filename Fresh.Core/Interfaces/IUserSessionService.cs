using Fresh.Core.DTOs.UserSession;

namespace Fresh.Core.Interfaces;

public interface IUserSessionService
{
    Task<IEnumerable<UserSessionResponse>> GetActiveSessionsAsync();
    Task<IEnumerable<UserSessionResponse>> GetHistoryByUserAsync(int userId);
    Task<IEnumerable<UserActionResponse>> GetSessionActionsAsync(int sessionId);
    Task<UserSessionResponse> StartSessionAsync(StartSessionRequest request);
    Task<UserSessionResponse?> EndSessionAsync(int sessionId);
    Task<UserSessionResponse?> UpdateLocationAsync(int sessionId, UpdateLocationRequest request);
    Task<UserSessionResponse?> AddIdleTimeAsync(int sessionId, UpdateIdleRequest request);
    Task<UserActionResponse> LogActionAsync(int sessionId, LogActionRequest request);
}

using Fresh.Core.DTOs.UserSession;

namespace Fresh.Core.Interfaces;

public interface IUserSessionService
{
    Task<IEnumerable<UserSessionResponse>> GetActiveSessionsAsync();
    Task<IEnumerable<UserSessionResponse>> GetHistoryByUserIdAsync(int userId, int top = 50);
    Task<IEnumerable<UserActionResponse>> GetSessionActionsAsync(int sessionId);
}
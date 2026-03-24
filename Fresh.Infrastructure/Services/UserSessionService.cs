using Fresh.Core.DTOs.UserSession;
using Fresh.Core.Entities;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fresh.Infrastructure.Services;

public class UserSessionService : IUserSessionService
{
    private readonly FreshDbContext _context;
    public UserSessionService(FreshDbContext context) { _context = context; }

    public async Task<IEnumerable<UserSessionResponse>> GetActiveSessionsAsync()
    {
        var sessions = await _context.UserSessions
            .Include(s => s.User)
            .Where(s => s.DisconnectedAt == null)
            .OrderByDescending(s => s.ConnectedAt)
            .ToListAsync();

        return sessions.Select(MapToResponse);
    }

    public async Task<IEnumerable<UserSessionResponse>> GetHistoryByUserIdAsync(int userId, int top = 50)
    {
        var sessions = await _context.UserSessions
            .Include(s => s.User)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.ConnectedAt)
            .Take(top)
            .ToListAsync();

        return sessions.Select(MapToResponse);
    }

    public async Task<IEnumerable<UserActionResponse>> GetSessionActionsAsync(int sessionId)
    {
        var actions = await _context.UserActions
            .Where(a => a.SessionId == sessionId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return actions.Select(a => new UserActionResponse
        {
            Id = a.Id,
            ActionType = a.ActionType,
            Description = a.Description,
            CreatedAt = a.CreatedAt
        });
    }

    private static UserSessionResponse MapToResponse(UserSession s) => new()
    {
        Id = s.Id,
        UserId = s.UserId,
        UserName = s.User?.Name ?? "Desconocido",
        ConnectionId = s.ConnectionId,
        ConnectedAt = s.ConnectedAt,
        DisconnectedAt = s.DisconnectedAt,
        TotalIdleSeconds = s.TotalIdleSeconds,
        LastKnownLocation = s.LastKnownLocation
    };
}
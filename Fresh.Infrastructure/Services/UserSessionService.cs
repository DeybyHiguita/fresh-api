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
            .Where(s => s.IsOnline)
            .OrderByDescending(s => s.ConnectedAt)
            .ToListAsync();

        return sessions.Select(Map);
    }

    public async Task<IEnumerable<UserSessionResponse>> GetHistoryByUserAsync(int userId)
    {
        var sessions = await _context.UserSessions
            .Include(s => s.User)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.ConnectedAt)
            .Take(50)
            .ToListAsync();

        return sessions.Select(Map);
    }

    public async Task<IEnumerable<UserActionResponse>> GetSessionActionsAsync(int sessionId)
    {
        var actions = await _context.UserActions
            .Where(a => a.SessionId == sessionId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return actions.Select(a => new UserActionResponse
        {
            Id          = a.Id,
            ActionType  = a.ActionType,
            Description = a.Description,
            CreatedAt   = a.CreatedAt,
        });
    }

    public async Task<UserSessionResponse> StartSessionAsync(StartSessionRequest request)
    {
        // Mark any previous open sessions for this user as offline
        var openSessions = await _context.UserSessions
            .Where(s => s.UserId == request.UserId && s.IsOnline)
            .ToListAsync();

        foreach (var s in openSessions)
        {
            s.IsOnline = false;
            s.DisconnectedAt = DateTimeOffset.UtcNow;
        }

        var session = new UserSession
        {
            UserId              = request.UserId,
            ConnectionId        = request.ConnectionId,
            ConnectedAt         = DateTimeOffset.UtcNow,
            IsOnline            = true,
            LastKnownLocation   = request.Location,
        };

        _context.UserSessions.Add(session);
        await _context.SaveChangesAsync();

        await _context.Entry(session).Reference(s => s.User).LoadAsync();
        return Map(session);
    }

    public async Task<UserSessionResponse?> EndSessionAsync(int sessionId)
    {
        var session = await _context.UserSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null) return null;

        session.IsOnline        = false;
        session.DisconnectedAt  = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();
        return Map(session);
    }

    public async Task<UserSessionResponse?> UpdateLocationAsync(int sessionId, UpdateLocationRequest request)
    {
        var session = await _context.UserSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null) return null;

        session.LastKnownLocation = request.Location;
        await _context.SaveChangesAsync();
        return Map(session);
    }

    public async Task<UserSessionResponse?> AddIdleTimeAsync(int sessionId, UpdateIdleRequest request)
    {
        var session = await _context.UserSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null) return null;

        session.TotalIdleSeconds += request.IdleSeconds;
        await _context.SaveChangesAsync();
        return Map(session);
    }

    public async Task<UserActionResponse> LogActionAsync(int sessionId, LogActionRequest request)
    {
        var action = new UserAction
        {
            SessionId   = sessionId,
            ActionType  = request.ActionType,
            Description = request.Description,
            CreatedAt   = DateTimeOffset.UtcNow,
        };

        _context.UserActions.Add(action);
        await _context.SaveChangesAsync();

        return new UserActionResponse
        {
            Id          = action.Id,
            ActionType  = action.ActionType,
            Description = action.Description,
            CreatedAt   = action.CreatedAt,
        };
    }

    private static UserSessionResponse Map(UserSession s) => new()
    {
        Id                  = s.Id,
        UserId              = s.UserId,
        UserName            = s.User?.Name ?? string.Empty,
        ConnectionId        = s.ConnectionId,
        ConnectedAt         = s.ConnectedAt,
        DisconnectedAt      = s.DisconnectedAt,
        TotalIdleSeconds    = s.TotalIdleSeconds,
        LastKnownLocation   = s.LastKnownLocation,
        IsOnline            = s.IsOnline,
    };
}

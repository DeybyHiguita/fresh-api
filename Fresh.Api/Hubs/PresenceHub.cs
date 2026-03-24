using System.Collections.Concurrent;
using System.Security.Claims;
using Fresh.Core.DTOs.UserSession;
using Fresh.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Fresh.Api.Hubs;

[Authorize]
public class PresenceHub : Hub
{
    private readonly IUserSessionService _sessionService;

    // Static: survives across Hub instances (one instance per connection)
    private static readonly ConcurrentDictionary<string, int> _connectionToSession = new();

    public PresenceHub(IUserSessionService sessionService)
    {
        _sessionService = sessionService;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId == 0)
        {
            Context.Abort();
            return;
        }

        var location = Context.GetHttpContext()?.Request.Query["location"].ToString();

        var session = await _sessionService.StartSessionAsync(new StartSessionRequest
        {
            UserId       = userId,
            ConnectionId = Context.ConnectionId,
            Location     = string.IsNullOrWhiteSpace(location) ? "/" : location,
        });

        _connectionToSession[Context.ConnectionId] = session.Id;

        // Log action: user connected
        await _sessionService.LogActionAsync(session.Id, new LogActionRequest
        {
            ActionType  = "connect",
            Description = $"Sesión iniciada desde {session.LastKnownLocation ?? "/"}",
        });

        await Clients.All.SendAsync("UserConnected", session);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_connectionToSession.TryRemove(Context.ConnectionId, out var sessionId))
        {
            // Log action before ending session
            await _sessionService.LogActionAsync(sessionId, new LogActionRequest
            {
                ActionType  = "disconnect",
                Description = "Sesión cerrada (desconexión del cliente)",
            });

            await _sessionService.EndSessionAsync(sessionId);
            await Clients.All.SendAsync("UserDisconnected", Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>Called by the client when the user navigates to a new page.</summary>
    public async Task UpdateLocation(string location)
    {
        if (!_connectionToSession.TryGetValue(Context.ConnectionId, out var sessionId))
            return;

        var updated = await _sessionService.UpdateLocationAsync(sessionId, new UpdateLocationRequest
        {
            Location = location,
        });

        if (updated != null)
        {
            // Log navigation action
            await _sessionService.LogActionAsync(sessionId, new LogActionRequest
            {
                ActionType  = "navigate",
                Description = $"Navegó a {location}",
            });

            await Clients.All.SendAsync("LocationUpdated", Context.ConnectionId, location);
        }
    }

    private int GetUserId()
    {
        var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null && int.TryParse(claim.Value, out var id) ? id : 0;
    }
}

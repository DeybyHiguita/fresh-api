using Fresh.Core.Entities;
using Fresh.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Fresh.Api.Hubs;

[Authorize]
public class PresenceHub : Hub
{
    private readonly FreshDbContext _context;

    public PresenceHub(FreshDbContext context) { _context = context; }

    public override async Task OnConnectedAsync()
    {
        // Obtener el ID del usuario desde el token JWT
        var userIdString = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? Context.User?.FindFirst("id")?.Value;

        if (int.TryParse(userIdString, out int userId))
        {
            // 🔥 LA SOLUCIÓN: Buscar sesiones anteriores "abiertas" de este mismo usuario y cerrarlas
            var oldSessions = _context.UserSessions
                .Where(s => s.UserId == userId && s.DisconnectedAt == null)
                .ToList();

            if (oldSessions.Any())
            {
                foreach (var old in oldSessions)
                {
                    old.DisconnectedAt = DateTimeOffset.UtcNow;
                    old.UpdatedAt = DateTimeOffset.UtcNow;

                    // Dejamos registro en la auditoría de que se cerró por una nueva conexión
                    _context.UserActions.Add(new UserAction
                    {
                        SessionId = old.Id,
                        ActionType = "Desconexión Forzada",
                        Description = "Sesión cerrada automáticamente porque el usuario abrió otra pestaña o recargó la página."
                    });
                }

                // Le avisamos al dashboard que esas sesiones viejas murieron
                await Clients.Others.SendAsync("UserDisconnected", userId);
            }

            // AHORA SÍ: Creamos la nueva sesión limpia
            var session = new UserSession
            {
                UserId = userId,
                ConnectionId = Context.ConnectionId
            };

            _context.UserSessions.Add(session);
            await _context.SaveChangesAsync();

            // Notificar a todos los demás que entró con su nueva sesión
            await Clients.Others.SendAsync("UserConnected", userId, session.Id);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var session = _context.UserSessions.FirstOrDefault(s => s.ConnectionId == Context.ConnectionId);
        if (session != null)
        {
            session.DisconnectedAt = DateTimeOffset.UtcNow;
            session.UpdatedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();
            
            await Clients.Others.SendAsync("UserDisconnected", session.UserId);
        }
        await base.OnDisconnectedAsync(exception);
    }

    // El frontend llamará a esto cada X minutos si el usuario no mueve el mouse
    public async Task ReportIdleTime(int idleSeconds)
    {
        var session = _context.UserSessions.FirstOrDefault(s => s.ConnectionId == Context.ConnectionId);
        if (session != null)
        {
            session.TotalIdleSeconds += idleSeconds;
            session.UpdatedAt = DateTimeOffset.UtcNow;
            
            _context.UserActions.Add(new UserAction 
            { 
                SessionId = session.Id, 
                ActionType = "Inactividad", 
                Description = $"Usuario inactivo reportado: {idleSeconds}s" 
            });
            
            await _context.SaveChangesAsync();
            await Clients.Others.SendAsync("UserWentIdle", session.UserId, idleSeconds);
        }
    }

    // El frontend llamará a esto al cambiar de ruta (ej: Entra a "Cajas")
    public async Task ReportLocation(string location)
    {
        var session = _context.UserSessions.FirstOrDefault(s => s.ConnectionId == Context.ConnectionId);
        if (session != null)
        {
            session.LastKnownLocation = location;
            session.UpdatedAt = DateTimeOffset.UtcNow;
            
            _context.UserActions.Add(new UserAction 
            { 
                SessionId = session.Id, 
                ActionType = "Navegación", 
                Description = $"Navegó a: {location}" 
            });
            
            await _context.SaveChangesAsync();
            await Clients.Others.SendAsync("UserChangedLocation", session.UserId, location);
        }
    }
}
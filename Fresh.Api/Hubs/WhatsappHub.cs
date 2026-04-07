using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Fresh.Api.Hubs;

/// <summary>
/// Hub para notificaciones del chat WhatsApp en tiempo real.
/// Solo admins reciben eventos de mensajes entrantes.
/// </summary>
[Authorize]
public class WhatsappHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var role = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
                ?? Context.User?.FindFirst("role")?.Value
                ?? string.Empty;

        if (role.Equals("admin", StringComparison.OrdinalIgnoreCase))
            await Groups.AddToGroupAsync(Context.ConnectionId, "admins");

        await base.OnConnectedAsync();
    }
}

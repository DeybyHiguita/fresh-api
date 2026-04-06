using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Fresh.Api.Hubs;

/// <summary>
/// Hub para notificaciones de órdenes en tiempo real.
/// Los administradores se suscriben al grupo "admins" al conectarse.
/// </summary>
[Authorize]
public class OrderHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var role = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
                ?? Context.User?.FindFirst("role")?.Value
                ?? string.Empty;

        if (role.Equals("admin", StringComparison.OrdinalIgnoreCase))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "admins");
        }

        await base.OnConnectedAsync();
    }
}

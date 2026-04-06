using Microsoft.AspNetCore.SignalR;

namespace Fresh.Api.Hubs;

/// <summary>
/// Hub para notificaciones de órdenes en tiempo real.
/// Los administradores se suscriben al grupo "admins" al conectarse.
/// </summary>
public class OrderHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        // El rol viene en los claims del JWT (ya validado por el middleware)
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

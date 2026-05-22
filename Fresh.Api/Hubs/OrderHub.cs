using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Fresh.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fresh.Api.Hubs;

/// <summary>
/// Hub para notificaciones de órdenes en tiempo real.
/// - Admins → grupo "admins"
/// - Usuarios con caja abierta → grupo "cash-open"
/// - Resto de usuarios → grupo "users"
/// </summary>
[Authorize]
public class OrderHub : Hub
{
    private readonly FreshDbContext _db;

    public OrderHub(FreshDbContext db)
    {
        _db = db;
    }

    public override async Task OnConnectedAsync()
    {
        var role = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
                ?? Context.User?.FindFirst("role")?.Value
                ?? string.Empty;

        if (role.Equals("admin", StringComparison.OrdinalIgnoreCase))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "admins");
        }
        else
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "users");

            // Si el usuario tiene una caja abierta, recibe también las alertas de transferencia
            var userIdClaim = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                           ?? Context.User?.FindFirst("sub")?.Value;

            if (int.TryParse(userIdClaim, out var userId))
            {
                var hasOpenCash = await _db.CashRegisters
                    .AnyAsync(r => r.OpenedById == userId && r.ClosingTime == null);

                if (hasOpenCash)
                    await Groups.AddToGroupAsync(Context.ConnectionId, "cash-open");
            }
        }

        await base.OnConnectedAsync();
    }
}

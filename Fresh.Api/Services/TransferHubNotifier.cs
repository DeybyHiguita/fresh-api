using Fresh.Core.DTOs.Order;
using Fresh.Core.Interfaces;
using Fresh.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Fresh.Api.Services;

public class TransferHubNotifier : ITransferHubNotifier
{
    private readonly IHubContext<OrderHub> _hub;

    public TransferHubNotifier(IHubContext<OrderHub> hub)
    {
        _hub = hub;
    }

    public async Task NotifyTransferReceivedAsync(TransferNotificationDto notification)
    {
        // Prioridad: cajeros con caja abierta (incluye admins que abrieron caja).
        // Los admins la reciben también como respaldo.
        // Si un admin tiene caja abierta está en ambos grupos; el frontend deduplica por transfer.id.
        await _hub.Clients.Group("cash-open").SendAsync("TransferReceived", notification);
        await _hub.Clients.Group("admins").SendAsync("TransferReceived", notification);
    }
}

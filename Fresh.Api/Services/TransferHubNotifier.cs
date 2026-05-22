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
        // Enviar a admins Y a usuarios con caja abierta
        await _hub.Clients.Group("admins").SendAsync("TransferReceived", notification);
        await _hub.Clients.Group("cash-open").SendAsync("TransferReceived", notification);
    }
}

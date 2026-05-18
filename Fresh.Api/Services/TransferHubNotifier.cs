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

    public Task NotifyTransferReceivedAsync(TransferNotificationDto notification)
        => _hub.Clients.Group("admins").SendAsync("TransferReceived", notification);
}

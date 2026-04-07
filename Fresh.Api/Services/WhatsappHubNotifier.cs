using Fresh.Core.Interfaces;
using Fresh.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Fresh.Api.Services;

public class WhatsappHubNotifier : IWhatsappHubNotifier
{
    private readonly IHubContext<WhatsappHub> _hub;

    public WhatsappHubNotifier(IHubContext<WhatsappHub> hub)
    {
        _hub = hub;
    }

    public Task NotifyNewMessageAsync(object payload)
        => _hub.Clients.Group("admins").SendAsync("NewWhatsappMessage", payload);
}

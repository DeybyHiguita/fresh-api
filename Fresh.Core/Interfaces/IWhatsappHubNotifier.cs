namespace Fresh.Core.Interfaces;

/// <summary>
/// Abstracción para notificar mensajes nuevos de WhatsApp por SignalR.
/// Rompe la dependencia circular entre Fresh.Infrastructure y Fresh.Api.
/// </summary>
public interface IWhatsappHubNotifier
{
    Task NotifyNewMessageAsync(object payload);
}

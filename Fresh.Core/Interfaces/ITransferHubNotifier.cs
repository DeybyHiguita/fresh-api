using Fresh.Core.DTOs.Order;

namespace Fresh.Core.Interfaces;

/// <summary>
/// Abstracción para notificar transferencias detectadas por SignalR.
/// </summary>
public interface ITransferHubNotifier
{
    Task NotifyTransferReceivedAsync(TransferNotificationDto notification);
}

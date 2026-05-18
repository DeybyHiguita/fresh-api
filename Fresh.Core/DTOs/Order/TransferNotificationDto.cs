namespace Fresh.Core.DTOs.Order;

/// <summary>
/// Representa una notificación de transferencia detectada desde WhatsApp.
/// </summary>
public record TransferNotificationDto(
    string Id,
    string Source,           // NEQUI, Bancolombia, Daviplata, etc.
    decimal Amount,
    string SenderName,
    string RawMessage,
    string ContactPhone,
    DateTimeOffset ReceivedAt,
    List<OrderMatchDto> MatchingOrders
);

/// <summary>
/// Orden candidata que coincide con el monto de la transferencia.
/// </summary>
public record OrderMatchDto(
    int OrderId,
    decimal Total,
    string? CustomerName,
    string? CustomerPhone,
    string PaymentMethod,
    string Status,
    DateTimeOffset CreatedAt
);

namespace Fresh.Core.DTOs.WhatsappChat;

public record WhatsappContactDto(
    int    Id,
    string WaId,
    string Name,
    string LastMessageAt,
    int    UnreadCount,
    string LastMessage
);

public record WhatsappMessageDto(
    int    Id,
    string Direction,
    string Body,
    string Status,
    string CreatedAt
);

public record SendMessageRequest(
    int    ContactId,
    string Body
);

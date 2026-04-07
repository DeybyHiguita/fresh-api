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
    int     Id,
    string  Direction,
    string  Body,
    string  Status,
    string  CreatedAt,
    string? MediaType = null,
    string? MediaId   = null,
    string? MediaName = null
);

public record SendMessageRequest(
    int    ContactId,
    string Body
);

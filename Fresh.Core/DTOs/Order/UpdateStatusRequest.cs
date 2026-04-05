namespace Fresh.Core.DTOs.Order;

public record UpdateStatusRequest(string Status, string? Notes = null);

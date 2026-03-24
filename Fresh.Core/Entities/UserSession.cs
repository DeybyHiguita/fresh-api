using System;
using System.Collections.Generic;

namespace Fresh.Core.Entities;

public class UserSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string ConnectionId { get; set; } = string.Empty;

    public DateTimeOffset ConnectedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DisconnectedAt { get; set; }

    public int TotalIdleSeconds { get; set; } = 0;
    public string? LastKnownLocation { get; set; } = "Dashboard";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Relaciones
    public User? User { get; set; }
    public ICollection<UserAction> Actions { get; set; } = [];
}
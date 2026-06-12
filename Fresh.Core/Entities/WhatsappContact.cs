namespace Fresh.Core.Entities;

public class WhatsappContact
{
    public int      Id            { get; set; }
    public string   WaId          { get; set; } = string.Empty;
    public string   Name          { get; set; } = string.Empty;
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;
    public int      UnreadCount   { get; set; } = 0;
    public bool     IsArchived    { get; set; } = false;
    public bool     IsPinned      { get; set; } = false;
    public DateTime CreatedAt     { get; set; } = DateTime.UtcNow;

    public ICollection<WhatsappMessage> Messages { get; set; } = new List<WhatsappMessage>();
}

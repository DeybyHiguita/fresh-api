namespace Fresh.Core.Entities;

public class WhatsappMessage
{
    public int      Id          { get; set; }
    public int      ContactId   { get; set; }
    public string   Direction   { get; set; } = "in";   // "in" | "out"
    public string   Body        { get; set; } = string.Empty;
    public string?  WaMessageId { get; set; }
    public string   Status      { get; set; } = "sent"; // sent | delivered | read | failed
    public string?  MediaType          { get; set; }            // image | document | audio | video
    public string?  MediaId            { get; set; }            // Meta media ID (para obtener URL)
    public string?  MediaName          { get; set; }            // nombre del archivo
    public string?  ReplyToWaMessageId { get; set; }            // WAMID del mensaje al que se responde
    public DateTime CreatedAt          { get; set; } = DateTime.UtcNow;

    public WhatsappContact Contact { get; set; } = null!;
}

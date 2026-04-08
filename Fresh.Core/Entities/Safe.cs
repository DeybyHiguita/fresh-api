namespace Fresh.Core.Entities;

public class Safe
{
    public int Id { get; set; }
    public decimal Balance { get; set; } = 0;
    public string SafeType { get; set; } = "caja_fuerte"; // "caja_fuerte" | "cuenta_bancaria"
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

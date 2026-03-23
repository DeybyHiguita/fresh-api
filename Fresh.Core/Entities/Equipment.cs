namespace Fresh.Core.Entities;

public class Equipment
{
    public int Id { get; set; }
    public int CategoryId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }

    public DateOnly? PurchaseDate { get; set; }
    public decimal PurchasePrice { get; set; } = 0m;

    public string Status { get; set; } = "Activo";
    public string? Location { get; set; }
    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public EquipmentCategory? Category { get; set; }
}

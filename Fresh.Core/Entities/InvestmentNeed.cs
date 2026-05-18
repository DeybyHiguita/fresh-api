namespace Fresh.Core.Entities;

public class InvestmentNeed
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Ítem relacionado (opcional)
    public string? ItemType { get; set; }
    public int? EquipmentId { get; set; }
    public int? PurchaseBatchId { get; set; }
    public int? ProductId { get; set; }

    public decimal TotalNeeded { get; set; }
    public string Status { get; set; } = "Pendiente"; // Pendiente | Aprobada | Rechazada

    public int CreatedById { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navegación
    public User? CreatedBy { get; set; }
    public Equipment? Equipment { get; set; }
    public PurchaseBatch? PurchaseBatch { get; set; }
    public Product? Product { get; set; }
    public ICollection<InvestmentNeedAssignment> Assignments { get; set; } = [];
    public ICollection<InvestmentNeedItem> Items { get; set; } = [];
}

namespace Fresh.Core.DTOs.Investment;

public class InvestmentItemResponse
{
    public int Id { get; set; }
    public int InvestmentId { get; set; }
    
    public string ItemType { get; set; } = string.Empty;
    public string ItemTypeName { get; set; } = string.Empty; // Equipo, Lote de Compra, Producto, Otro
    
    public int? EquipmentId { get; set; }
    public string? EquipmentName { get; set; }
    
    public int? PurchaseBatchId { get; set; }
    public string? PurchaseBatchName { get; set; }
    
    public int? ProductId { get; set; }
    public string? ProductName { get; set; }
    
    public string? Description { get; set; }
    public decimal Amount { get; set; }

    public decimal? Quantity { get; set; }
    public decimal? UnitPrice { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

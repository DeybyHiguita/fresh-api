using System.ComponentModel.DataAnnotations;

namespace Fresh.Core.DTOs.Investment;

public class InvestmentItemRequest
{
    [Required]
    public int InvestmentId { get; set; }
    
    [Required]
    public string ItemType { get; set; } = string.Empty; // equipment, purchase_batch, product, other
    
    public int? EquipmentId { get; set; }
    public int? PurchaseBatchId { get; set; }
    public int? ProductId { get; set; }
    
    public string? Description { get; set; }
    
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a 0")]
    public decimal Amount { get; set; }
}

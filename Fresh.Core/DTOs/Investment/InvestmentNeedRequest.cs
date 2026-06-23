using System.ComponentModel.DataAnnotations;

namespace Fresh.Core.DTOs.Investment;

public class InvestmentNeedRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [MaxLength(50)]
    public string? ItemType { get; set; }

    public int? EquipmentId { get; set; }
    public int? PurchaseBatchId { get; set; }
    public int? ProductId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "El total debe ser mayor a 0")]
    public decimal TotalNeeded { get; set; }

    [Required]
    public int CreatedById { get; set; }

    public List<InvestmentNeedAssignmentRequest> Assignments { get; set; } = [];
    public List<InvestmentNeedItemRequest> Items { get; set; } = [];
}

public class InvestmentNeedItemRequest
{
    [MaxLength(50)]
    public string? ItemType { get; set; }

    public int? EquipmentId { get; set; }
    public int? PurchaseBatchId { get; set; }
    public int? ProductId { get; set; }

    [MaxLength(300)]
    public string? Description { get; set; }

    public decimal? Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? EstimatedCost { get; set; }
}

public class InvestmentNeedAssignmentRequest
{
    [Required]
    public int InvestorId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a 0")]
    public decimal SuggestedAmount { get; set; }

    public string? Notes { get; set; }
}

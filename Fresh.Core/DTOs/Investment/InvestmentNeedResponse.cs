namespace Fresh.Core.DTOs.Investment;

public class InvestmentNeedResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ItemType { get; set; }

    public int? EquipmentId { get; set; }
    public string? EquipmentName { get; set; }
    public int? PurchaseBatchId { get; set; }
    public string? PurchaseBatchName { get; set; }
    public int? ProductId { get; set; }
    public string? ProductName { get; set; }

    public decimal TotalNeeded { get; set; }
    public decimal AssignedTotal { get; set; }
    public string Status { get; set; } = string.Empty;

    public int CreatedById { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public List<InvestmentNeedAssignmentResponse> Assignments { get; set; } = [];
    public List<InvestmentNeedItemResponse> Items { get; set; } = [];
}

public class InvestmentNeedItemResponse
{
    public int Id { get; set; }
    public int NeedId { get; set; }
    public string? ItemType { get; set; }
    public int? EquipmentId { get; set; }
    public string? EquipmentName { get; set; }
    public int? PurchaseBatchId { get; set; }
    public string? PurchaseBatchName { get; set; }
    public int? ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? Description { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? EstimatedCost { get; set; }
}

public class InvestmentNeedAssignmentResponse
{
    public int Id { get; set; }
    public int NeedId { get; set; }
    public int InvestorId { get; set; }
    public string InvestorName { get; set; } = string.Empty;
    public decimal SuggestedAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public int? InvestmentId { get; set; }
}

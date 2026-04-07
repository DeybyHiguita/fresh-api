namespace Fresh.Core.DTOs.CustomerCredit;

public class CreditOrderItemResponse
{
    public int MenuItemId { get; set; }
    public string MenuItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
    public string? ItemNotes { get; set; }
}

public class CreditOrderResponse
{
    public int OrderId { get; set; }
    public string? CustomerName { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsCreditPaid { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? Notes { get; set; }
    public List<CreditOrderItemResponse> Items { get; set; } = [];
}

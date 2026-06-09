namespace Fresh.Core.DTOs.PurchaseBatch;

public class PublicBatchResponse
{
    public string BatchName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal Total { get; set; }
    public List<PublicBatchItem> Items { get; set; } = [];
}

public class PublicBatchItem
{
    public string ProductName { get; set; } = string.Empty;
    public string ProductUnit { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalValue { get; set; }
}

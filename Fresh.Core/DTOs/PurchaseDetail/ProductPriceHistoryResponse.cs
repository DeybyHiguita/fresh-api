namespace Fresh.Core.DTOs.PurchaseDetail;

public class ProductPriceHistoryResponse
{
    public int BatchId { get; set; }
    public string BatchName { get; set; } = string.Empty;
    public DateOnly BatchDate { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalValue { get; set; }
    public string ProductUnit { get; set; } = string.Empty;
}

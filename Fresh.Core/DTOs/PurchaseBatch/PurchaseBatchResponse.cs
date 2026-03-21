using Fresh.Core.DTOs.PurchaseDetail;

namespace Fresh.Core.DTOs.PurchaseBatch;

public class PurchaseBatchResponse
{
    public int Id { get; set; }
    public string BatchName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<PurchaseDetailResponse> Details { get; set; } = [];
}

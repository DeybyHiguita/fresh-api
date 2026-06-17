namespace Fresh.Core.Entities;

public class PurchaseBatch
{
    public int Id { get; set; }
    public int StoreId { get; set; }
    public string BatchName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Relaciones
    public Store? Store { get; set; }
    public ICollection<PurchaseDetail> PurchaseDetails { get; set; } = [];
    public ICollection<PurchaseBatchInvoice> Invoices { get; set; } = [];
}

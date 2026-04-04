using Fresh.Core.DTOs.PurchaseBatch;
using Fresh.Core.DTOs.PurchaseDetail;

namespace Fresh.Core.Interfaces;

public interface IPurchaseBatchService
{
    Task<IEnumerable<PurchaseBatchResponse>> GetAllAsync();
    Task<(IEnumerable<PurchaseBatchResponse> Items, int Total)> GetPagedAsync(int skip, int take);
    Task<(IEnumerable<PurchaseBatchSummary> Items, int Total)> GetSummariesAsync(int skip, int take, string? search);
    Task<PurchaseBatchResponse?> GetByIdAsync(int id);
    Task<PurchaseBatchResponse> CreateAsync(PurchaseBatchRequest request);
    Task<PurchaseBatchResponse?> UpdateAsync(int id, PurchaseBatchRequest request);
    Task<bool> DeleteAsync(int id);

    // Detalle dentro del lote
    Task<PurchaseDetailResponse> AddDetailAsync(int batchId, PurchaseDetailRequest request);
    Task<PurchaseDetailResponse?> UpdateDetailAsync(int batchId, int detailId, PurchaseDetailRequest request);
    Task<bool> RemoveDetailAsync(int batchId, int detailId);

    // Actualización masiva de precios
    Task BatchUpdateDetailsAsync(List<BatchUpdateItem> updates);

    // Historial de precios de un producto
    Task<IEnumerable<ProductPriceHistoryResponse>> GetProductPriceHistoryAsync(int productId);
}

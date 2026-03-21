using Fresh.Core.DTOs.PurchaseBatch;
using Fresh.Core.DTOs.PurchaseDetail;

namespace Fresh.Core.Interfaces;

public interface IPurchaseBatchService
{
    Task<IEnumerable<PurchaseBatchResponse>> GetAllAsync();
    Task<PurchaseBatchResponse?> GetByIdAsync(int id);
    Task<PurchaseBatchResponse> CreateAsync(PurchaseBatchRequest request);
    Task<PurchaseBatchResponse?> UpdateAsync(int id, PurchaseBatchRequest request);
    Task<bool> DeleteAsync(int id);

    // Detalle dentro del lote
    Task<PurchaseDetailResponse> AddDetailAsync(int batchId, PurchaseDetailRequest request);
    Task<PurchaseDetailResponse?> UpdateDetailAsync(int batchId, int detailId, PurchaseDetailRequest request);
    Task<bool> RemoveDetailAsync(int batchId, int detailId);
}

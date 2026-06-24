using Fresh.Core.DTOs.Investment;

namespace Fresh.Core.Interfaces;

public interface IInvestmentNeedService
{
    Task<IEnumerable<InvestmentNeedResponse>> GetAllAsync();
    Task<InvestmentNeedResponse?> GetByIdAsync(int id);
    Task<InvestmentNeedResponse> CreateAsync(InvestmentNeedRequest request);
    Task<InvestmentNeedResponse> UpdateAsync(int id, InvestmentNeedRequest request);
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// Aprueba la solicitud: crea una inversión con estado "Pendiente" por cada
    /// asignación de la solicitud, genera un lote de compras con los productos
    /// enlazados y cambia el estado de la solicitud a "Aprobada".
    /// </summary>
    Task<List<InvestmentResponse>> ApproveAsync(int needId, int storeId = 0);

    /// <summary>
    /// Rechaza la solicitud sin crear inversiones.
    /// </summary>
    Task<bool> RejectAsync(int needId);
}

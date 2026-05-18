using Fresh.Core.DTOs.Investment;

namespace Fresh.Core.Interfaces;

public interface IProfitDistributionService
{
    Task<IEnumerable<ProfitDistributionResponse>> GetAllAsync();
    Task<ProfitDistributionResponse?> GetByIdAsync(int id);
    Task<ProfitDistributionResponse> CreateAsync(int userId, ProfitDistributionRequest request);
    Task DeleteAsync(int id);
}

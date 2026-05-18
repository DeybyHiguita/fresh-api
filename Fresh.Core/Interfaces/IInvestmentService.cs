using Fresh.Core.DTOs.Investment;

namespace Fresh.Core.Interfaces;

public interface IInvestmentService
{
    Task<IEnumerable<InvestmentResponse>> GetAllAsync();
    Task<InvestmentResponse?> GetByIdAsync(int id);
    Task<IEnumerable<InvestmentResponse>> GetByInvestorAsync(int investorId);
    Task<InvestmentResponse> CreateAsync(InvestmentRequest request);
    Task<InvestmentResponse?> UpdateAsync(int id, InvestmentRequest request);
    Task<bool> DeleteAsync(int id);
    
    // Items
    Task<InvestmentItemResponse> AddItemAsync(InvestmentItemRequest request);
    Task<bool> RemoveItemAsync(int itemId);
    Task<InvestmentItemResponse?> UpdateItemAsync(int itemId, InvestmentItemRequest request);
}

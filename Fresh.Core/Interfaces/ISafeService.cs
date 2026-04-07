using Fresh.Core.DTOs.Safe;

namespace Fresh.Core.Interfaces;

public interface ISafeService
{
    Task<SafeResponse> GetSafeAsync();
    Task<IEnumerable<SafeTransactionResponse>> GetTransactionsAsync(int? limit = null);
    Task<SafeTransactionResponse> AddExpenseAsync(SafeExpenseRequest request);
    Task<SafeTransactionResponse> AddDepositAsync(SafeDepositRequest request);
}

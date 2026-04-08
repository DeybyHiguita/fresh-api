using Fresh.Core.DTOs.Safe;

namespace Fresh.Core.Interfaces;

public interface ISafeService
{
    Task<SafeResponse> GetSafeAsync(string safeType = "caja_fuerte");
    Task<IEnumerable<SafeTransactionResponse>> GetTransactionsAsync(string safeType = "caja_fuerte", int? limit = null);
    Task<SafeTransactionResponse> AddExpenseAsync(SafeExpenseRequest request);
    Task<SafeTransactionResponse> AddDepositAsync(SafeDepositRequest request);
}

using Fresh.Core.DTOs.CustomerCredit;

namespace Fresh.Core.Interfaces;

public interface ICustomerCreditService
{
    Task<CustomerCreditResponse?> GetByCustomerIdAsync(int customerId);
    Task<CustomerCreditResponse> CreateOrUpdateConfigAsync(CustomerCreditRequest request);
    Task<CustomerCreditResponse?> RegisterPaymentAsync(int id, CreditPaymentRequest request);
    Task<CustomerCreditResponse?> RegisterPurchaseAsync(int customerId, decimal purchaseAmount);
    Task<IEnumerable<CreditTransactionResponse>> GetTransactionsAsync(int customerId);
    Task<IEnumerable<CreditOrderResponse>> GetCreditOrdersAsync(int customerId);
    Task<CustomerCreditResponse> PayOrdersAsync(int creditId, PayOrdersRequest request);
}
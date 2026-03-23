using Fresh.Core.DTOs.Customer;

namespace Fresh.Core.Interfaces;

public interface ICustomerService
{
    Task<IEnumerable<CustomerResponse>> GetAllAsync(bool onlyActive = true);
    Task<CustomerResponse?> GetByIdAsync(int id);
    Task<CustomerResponse> CreateAsync(CustomerRequest request);
    Task<CustomerResponse?> UpdateAsync(int id, CustomerRequest request);
    Task<bool> DeleteAsync(int id);
}
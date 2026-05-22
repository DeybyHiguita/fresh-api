using Fresh.Core.DTOs.EmployeeAffiliation;

namespace Fresh.Core.Interfaces;

public interface IEmployeeAffiliationService
{
    Task<IEnumerable<EmployeeAffiliationResponse>> GetByEmployeeAsync(int employeeId);
    Task<EmployeeAffiliationResponse?> GetByIdAsync(int id);
    Task<EmployeeAffiliationResponse?> GetByTypeAsync(int employeeId, string affiliationType);
    Task<EmployeeAffiliationResponse> CreateOrUpdateAsync(int employeeId, EmployeeAffiliationRequest request);
    Task<bool> DeleteAsync(int id);
}

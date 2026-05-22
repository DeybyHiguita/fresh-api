using Fresh.Core.DTOs.EmployeeChild;

namespace Fresh.Core.Interfaces;

public interface IEmployeeChildService
{
    Task<IEnumerable<EmployeeChildResponse>> GetByEmployeeAsync(int employeeId);
    Task<EmployeeChildResponse?> GetByIdAsync(int id);
    Task<EmployeeChildResponse> CreateAsync(int employeeId, EmployeeChildRequest request);
    Task<EmployeeChildResponse?> UpdateAsync(int id, EmployeeChildRequest request);
    Task<bool> ToggleActiveAsync(int id);
    Task<bool> DeleteAsync(int id);
}

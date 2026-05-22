using Fresh.Core.DTOs.Employee;

namespace Fresh.Core.Interfaces;

public interface IEmployeeService
{
    Task<IEnumerable<EmployeeResponse>> GetAllAsync();
    Task<IEnumerable<EmployeeResponse>> GetActiveAsync();
    Task<EmployeeResponse?> GetByIdAsync(int id);
    Task<EmployeeResponse?> GetByUserIdAsync(int userId);
    Task<EmployeeResponse?> GetByDocumentAsync(string documentType, string documentNumber);
    Task<EmployeeResponse> CreateAsync(EmployeeRequest request);
    Task<EmployeeResponse?> UpdateAsync(int id, EmployeeRequest request);
    Task<EmployeeResponse?> LinkUserAsync(int employeeId, LinkUserRequest request);
    Task<EmployeeResponse?> UnlinkUserAsync(int employeeId);
    Task<EmployeeResponse?> TerminateAsync(int id, TerminateEmployeeRequest request);
    Task<EmployeeResponse?> ReactivateAsync(int id);
    Task<bool> DeleteAsync(int id);
    
    // Certificado laboral
    Task<byte[]> GenerateLaborCertificateAsync(int employeeId);
}

using Fresh.Core.DTOs.EmployeeDocumentType;

namespace Fresh.Core.Interfaces;

public interface IEmployeeDocumentTypeService
{
    Task<IEnumerable<EmployeeDocumentTypeResponse>> GetAllAsync();
    Task<IEnumerable<EmployeeDocumentTypeResponse>> GetActiveAsync();
    Task<IEnumerable<EmployeeDocumentTypeResponse>> GetByAppliesToAsync(string appliesTo);
    Task<EmployeeDocumentTypeResponse?> GetByIdAsync(int id);
    Task<EmployeeDocumentTypeResponse> CreateAsync(EmployeeDocumentTypeRequest request);
    Task<EmployeeDocumentTypeResponse?> UpdateAsync(int id, EmployeeDocumentTypeRequest request);
    Task<bool> ToggleActiveAsync(int id);
    Task<bool> DeleteAsync(int id);
}

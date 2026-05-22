using Fresh.Core.DTOs.EmployeeDocument;
using Microsoft.AspNetCore.Http;

namespace Fresh.Core.Interfaces;

public interface IEmployeeDocumentService
{
    Task<IEnumerable<EmployeeDocumentResponse>> GetByEmployeeAsync(int employeeId);
    Task<EmployeeDocumentResponse?> GetByIdAsync(int id);
    Task<EmployeeDocumentResponse> UploadAsync(int employeeId, IFormFile file, EmployeeDocumentRequest request, int uploadedBy);
    Task<EmployeeDocumentResponse?> UpdateAsync(int id, IFormFile? file, EmployeeDocumentRequest request);
    Task<EmployeeDocumentResponse?> VerifyAsync(int id, int verifiedBy);
    Task<bool> DeleteAsync(int id);
    Task<(byte[] content, string fileName, string contentType)> DownloadAsync(int id);
}

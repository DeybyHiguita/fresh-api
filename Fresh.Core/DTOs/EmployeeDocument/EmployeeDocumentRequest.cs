namespace Fresh.Core.DTOs.EmployeeDocument;

public class EmployeeDocumentRequest
{
    public int DocumentTypeId { get; set; }
    public string? Notes { get; set; }
    public DateTime? ExpirationDate { get; set; }
}

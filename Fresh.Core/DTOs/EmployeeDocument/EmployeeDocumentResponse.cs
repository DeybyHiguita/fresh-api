namespace Fresh.Core.DTOs.EmployeeDocument;

public class EmployeeDocumentResponse
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int DocumentTypeId { get; set; }
    public string DocumentTypeName { get; set; } = string.Empty;
    
    // Información del archivo
    public string FileName { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
    public string? FilePath { get; set; }
    public int? FileSize { get; set; }
    public string? MimeType { get; set; }
    
    // Google Drive
    public string? GoogleDriveFileId { get; set; }
    public string? GoogleDriveLink { get; set; }
    
    // Metadatos
    public string? Notes { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public bool IsExpired => ExpirationDate.HasValue && ExpirationDate.Value < DateTime.UtcNow;
    public bool IsVerified { get; set; }
    public string? VerifiedByName { get; set; }
    public DateTime? VerifiedAt { get; set; }
    
    // Auditoría
    public string? UploadedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Helper
    public string FileSizeDisplay => FileSize switch
    {
        null => "N/A",
        < 1024 => $"{FileSize} B",
        < 1048576 => $"{FileSize / 1024} KB",
        _ => $"{FileSize / 1048576} MB"
    };
}

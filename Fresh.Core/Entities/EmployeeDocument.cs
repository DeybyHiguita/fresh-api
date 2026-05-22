namespace Fresh.Core.Entities;

/// <summary>
/// Documentos subidos de empleados (almacenados en Google Drive)
/// </summary>
public class EmployeeDocument
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public int DocumentTypeId { get; set; }
    
    // Información del archivo
    public string FileName { get; set; } = string.Empty;      // Nombre con GUID en Drive
    public string OriginalName { get; set; } = string.Empty;  // Nombre original
    public string? FilePath { get; set; }                      // Legacy, puede ser null
    public int? FileSize { get; set; }
    public string? MimeType { get; set; }
    
    // Google Drive
    public string? GoogleDriveFileId { get; set; }            // ID del archivo en Google Drive
    public string? GoogleDriveLink { get; set; }              // Link para visualizar
    
    // Metadatos
    public string? Notes { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public bool IsVerified { get; set; } = false;
    public int? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }
    
    // Auditoría
    public int? UploadedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navegación
    public Employee? Employee { get; set; }
    public EmployeeDocumentType? DocumentType { get; set; }
}

namespace Fresh.Core.Entities;

/// <summary>
/// Documentos de los hijos de empleados
/// </summary>
public class EmployeeChildDocument
{
    public int Id { get; set; }
    public int ChildId { get; set; }
    public int DocumentTypeId { get; set; }
    
    // Información del archivo
    public string FileName { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int? FileSize { get; set; }
    public string? MimeType { get; set; }
    
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
    public EmployeeChild? Child { get; set; }
    public EmployeeDocumentType? DocumentType { get; set; }
}

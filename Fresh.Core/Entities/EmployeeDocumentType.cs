namespace Fresh.Core.Entities;

/// <summary>
/// Tipos de documentos configurables para empleados e hijos
/// </summary>
public class EmployeeDocumentType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsRequired { get; set; } = false;
    public string AppliesTo { get; set; } = "employee"; // employee, child
    public int MaxFileSize { get; set; } = 5242880; // 5MB
    public string AllowedFormats { get; set; } = "pdf,jpg,jpeg,png";
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

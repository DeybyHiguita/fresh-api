namespace Fresh.Core.DTOs.EmployeeDocumentType;

public class EmployeeDocumentTypeResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsRequired { get; set; }
    public string AppliesTo { get; set; } = string.Empty;
    public int MaxFileSize { get; set; }
    public string AllowedFormats { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Computed para facilitar el frontend
    public string MaxFileSizeDisplay => MaxFileSize switch
    {
        < 1024 => $"{MaxFileSize} B",
        < 1048576 => $"{MaxFileSize / 1024} KB",
        _ => $"{MaxFileSize / 1048576} MB"
    };
    
    public string AppliesToDisplay => AppliesTo switch
    {
        "employee" => "Empleado",
        "child" => "Hijo",
        _ => AppliesTo
    };
}

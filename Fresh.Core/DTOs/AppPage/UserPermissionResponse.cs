namespace Fresh.Core.DTOs.UserPermission;

public class UserPermissionResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int PageId { get; set; }
    public string PageName { get; set; } = string.Empty;
    public string PageRoute { get; set; } = string.Empty;
    public string? PageIcon { get; set; }
    public bool CanAccess { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
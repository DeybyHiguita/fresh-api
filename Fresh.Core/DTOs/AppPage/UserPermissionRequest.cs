using System.ComponentModel.DataAnnotations;

namespace Fresh.Core.DTOs.UserPermission;

public class UserPermissionRequest
{
    [Required]
    public int PageId { get; set; }

    [Required]
    public bool CanAccess { get; set; }
}
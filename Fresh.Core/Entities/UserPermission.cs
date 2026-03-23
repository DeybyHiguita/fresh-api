namespace Fresh.Core.Entities;

public class UserPermission
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Page { get; set; } = string.Empty;
    public bool CanAccess { get; set; } = false;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}

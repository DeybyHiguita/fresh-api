namespace Fresh.Core.Entities;

public class UserPermission
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int PageId { get; set; }
    public bool CanAccess { get; set; } = false;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public User? User { get; set; }
    public AppPage? Page { get; set; }
}
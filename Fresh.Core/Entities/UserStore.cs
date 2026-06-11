namespace Fresh.Core.Entities;

public class UserStore
{
    public int UserId { get; set; }
    public int StoreId { get; set; }
    public bool IsDefault { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Store Store { get; set; } = null!;
}

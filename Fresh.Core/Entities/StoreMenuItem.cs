namespace Fresh.Core.Entities;

public class StoreMenuItem
{
    public int StoreId { get; set; }
    public int MenuItemId { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Store Store { get; set; } = null!;
    public MenuItem MenuItem { get; set; } = null!;
}

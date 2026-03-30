namespace Fresh.Core.DTOs.MenuItem;

public class ReorderMenuItemsRequest
{
    public List<SortOrderItem> Items { get; set; } = new();

    public class SortOrderItem
    {
        public int Id { get; set; }
        public int SortOrder { get; set; }
    }
}

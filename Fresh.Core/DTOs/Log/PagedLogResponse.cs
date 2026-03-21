namespace Fresh.Core.DTOs.Log;

public class PagedLogResponse
{
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public IEnumerable<LogResponse> Items { get; set; } = [];
}

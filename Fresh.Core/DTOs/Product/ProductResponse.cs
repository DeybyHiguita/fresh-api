namespace Fresh.Core.DTOs.Product;

public class ProductResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string UnitMeasure { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

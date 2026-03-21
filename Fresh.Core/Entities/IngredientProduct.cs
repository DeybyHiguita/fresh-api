namespace Fresh.Core.Entities;

public class IngredientProduct
{
    public int Id { get; set; }
    public int IngredientId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Ingredient Ingredient { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
using Fresh.Core.Entities;

public class RecipeDetail
{
    public int Id { get; set; }
    public int RecipeId { get; set; }

    public int? IngredientId { get; set; }
    public Ingredient? Ingredient { get; set; }

    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
}
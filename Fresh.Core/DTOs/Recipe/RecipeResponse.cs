namespace Fresh.Core.DTOs.Recipe;

public class RecipeResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Instructions { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public List<RecipeIngredientResponse> Ingredients { get; set; } = new();
    public List<RecipeDetailResponse> Details { get; set; } = new();
}

public class RecipeIngredientResponse
{
    public int Id { get; set; }
    public int IngredientId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
}

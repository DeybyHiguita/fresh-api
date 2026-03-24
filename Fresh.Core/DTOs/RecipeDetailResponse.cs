namespace Fresh.Core.DTOs.Recipe;

public class RecipeDetailResponse
{
    public int Id { get; set; }
    public int? IngredientId { get; set; }
    public string? IngredientName { get; set; }

    public int? ProductId { get; set; }
    public string? ProductName { get; set; }

    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;

    public string DisplayName => IngredientName ?? ProductName ?? "Desconocido";
    public string Type => IngredientId.HasValue ? "Ingrediente" : "Producto";
}
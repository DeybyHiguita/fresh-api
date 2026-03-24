using System.ComponentModel.DataAnnotations;

namespace Fresh.Core.DTOs.Recipe;

public class RecipeRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public string? Instructions { get; set; }

    [Required]
    public int CategoryId { get; set; }

    // Nuevo sistema unificado (ingredientes + productos)
    public List<RecipeDetailRequest> Details { get; set; } = new();

    // Compatibilidad con el sistema antiguo (solo ingredientes)
    public List<RecipeIngredientRequest> Ingredients { get; set; } = new();
}

public class RecipeIngredientRequest
{
    [Required]
    public int IngredientId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Quantity { get; set; }

    [Required]
    [MaxLength(20)]
    public string Unit { get; set; } = string.Empty;
}

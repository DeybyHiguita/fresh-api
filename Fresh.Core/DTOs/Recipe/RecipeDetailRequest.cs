using System.ComponentModel.DataAnnotations;

namespace Fresh.Core.DTOs.Recipe;

public class RecipeDetailRequest
{
    public int? IngredientId { get; set; }
    public int? ProductId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
    public decimal Quantity { get; set; }

    [Required]
    [MaxLength(20)]
    public string Unit { get; set; } = string.Empty;
}
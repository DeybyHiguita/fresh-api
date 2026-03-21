using System.ComponentModel.DataAnnotations;

namespace Fresh.Core.DTOs.Ingredient;

public class IngredientRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Unit { get; set; } = string.Empty;

    public List<IngredientConsumptionRequest> Consumptions { get; set; } = new();
}

public class IngredientConsumptionRequest
{
    [Required]
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Required]
    [Range(typeof(decimal), "0.01", "999999999")]
    public decimal Quantity { get; set; }
}

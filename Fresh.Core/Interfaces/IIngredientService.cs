using Fresh.Core.DTOs.Ingredient;

namespace Fresh.Core.Interfaces;

public interface IIngredientService
{
    Task<IEnumerable<IngredientResponse>> GetAllAsync();
    Task<IngredientResponse?> GetByIdAsync(int id);
    Task<IngredientResponse> CreateAsync(IngredientRequest request);
    Task<IngredientResponse?> UpdateAsync(int id, IngredientRequest request);
    Task<bool> DeleteAsync(int id);
}

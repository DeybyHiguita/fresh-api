using Fresh.Core.DTOs.Recipe;

namespace Fresh.Core.Interfaces;

public interface IRecipeService
{
    Task<List<RecipeResponse>> GetAllAsync();
    Task<RecipeResponse?> GetByIdAsync(int id);
    Task<RecipeResponse> CreateAsync(RecipeRequest request);
    Task<RecipeResponse?> UpdateAsync(int id, RecipeRequest request);
    Task<bool> DeleteAsync(int id);
    Task<List<RecipeResponse>> GetByNameAsync(string name); // Buscar recetas por nombre
}

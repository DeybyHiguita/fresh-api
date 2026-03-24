using Fresh.Core.DTOs.Recipe;
using Fresh.Core.Entities;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fresh.Infrastructure.Services;

public class RecipeService : IRecipeService
{
    private readonly FreshDbContext _context;

    public RecipeService(FreshDbContext context)
    {
        _context = context;
    }

    public async Task<List<RecipeResponse>> GetAllAsync()
    {
        return await _context.Recipes
            .Include(r => r.Category)
            .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Ingredient)
            .Select(r => MapToResponse(r))
            .ToListAsync();
    }

    public async Task<RecipeResponse?> GetByIdAsync(int id)
    {
        var recipe = await _context.Recipes
            .Include(r => r.Category)
            .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Ingredient)
            .FirstOrDefaultAsync(r => r.Id == id);

        return recipe == null ? null : MapToResponse(recipe);
    }

    public async Task<RecipeResponse> CreateAsync(RecipeRequest request)
    {
        var recipe = new Recipe
        {
            Name = request.Name,
            Description = request.Description,
            Instructions = request.Instructions,
            CategoryId = request.CategoryId,
            RecipeIngredients = request.Ingredients.Select(i => new RecipeIngredient
            {
                IngredientId = i.IngredientId,
                Quantity = i.Quantity,
                Unit = i.Unit
            }).ToList()
        };

        _context.Recipes.Add(recipe);
        await _context.SaveChangesAsync();

        return (await GetByIdAsync(recipe.Id))!;
    }

    public async Task<RecipeResponse?> UpdateAsync(int id, RecipeRequest request)
    {
        var recipe = await _context.Recipes
            .Include(r => r.RecipeIngredients)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (recipe == null) return null;

        recipe.Name = request.Name;
        recipe.Description = request.Description;
        recipe.Instructions = request.Instructions;
        recipe.CategoryId = request.CategoryId;
        recipe.UpdatedAt = DateTime.UtcNow;

        // Reemplazar ingredientes
        _context.RecipeIngredients.RemoveRange(recipe.RecipeIngredients);
        recipe.RecipeIngredients = request.Ingredients.Select(i => new RecipeIngredient
        {
            IngredientId = i.IngredientId,
            Quantity = i.Quantity,
            Unit = i.Unit
        }).ToList();

        await _context.SaveChangesAsync();

        return (await GetByIdAsync(id))!;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var recipe = await _context.Recipes.FindAsync(id);
        if (recipe == null) return false;

        _context.Recipes.Remove(recipe);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<RecipeResponse>> GetByNameAsync(string name)
    {
        var query = _context.Recipes
            .Include(r => r.Category)
            .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Ingredient)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(name))
        {
            var lowered = name.Trim().ToLower();
            query = query.Where(r => r.Name.ToLower().Contains(lowered));
        }

        return await query.Select(r => MapToResponse(r)).ToListAsync();
    }

    private static RecipeResponse MapToResponse(Recipe recipe)
    {
        return new RecipeResponse
        {
            Id = recipe.Id,
            Name = recipe.Name,
            Description = recipe.Description,
            Instructions = recipe.Instructions,
            CategoryName = recipe.Category.Name,
            CategoryId = recipe.CategoryId,
            Ingredients = recipe.RecipeIngredients.Select(ri => new RecipeIngredientResponse
            {
                Id = ri.Id,
                IngredientId = ri.IngredientId,
                IngredientName = ri.Ingredient.Name,
                Quantity = ri.Quantity,
                Unit = ri.Unit
            }).ToList()
        };
    }

    public async Task<RecipeDetailResponse> AddDetailAsync(int recipeId, RecipeDetailRequest request)
    {
        // 1. Validar que la receta exista
        var recipe = await _context.Recipes.AnyAsync(r => r.Id == recipeId);
        if (!recipe) throw new KeyNotFoundException("Receta no encontrada.");

        // 2. Validar integridad: o es ingrediente o es producto, pero no ambos ni ninguno
        if (request.IngredientId.HasValue && request.ProductId.HasValue)
            throw new ArgumentException("Un detalle no puede ser ingrediente y producto al mismo tiempo.");

        if (!request.IngredientId.HasValue && !request.ProductId.HasValue)
            throw new ArgumentException("Debe especificar un ingrediente o un producto.");

        // 3. Crear entidad
        var detail = new RecipeDetail
        {
            RecipeId = recipeId,
            IngredientId = request.IngredientId,
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            Unit = request.Unit,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.RecipeDetails.Add(detail);
        await _context.SaveChangesAsync();

        // 4. Retornar el DTO mapeado (incluyendo los nombres de navegación)
        var savedDetail = await _context.RecipeDetails
            .Include(d => d.Ingredient)
            .Include(d => d.Product)
            .FirstAsync(d => d.Id == detail.Id);

        return MapToDetailResponse(savedDetail);
    }

    public async Task<bool> RemoveDetailAsync(int detailId)
    {
        var detail = await _context.RecipeDetails.FindAsync(detailId);
        if (detail == null) return false;

        _context.RecipeDetails.Remove(detail);
        await _context.SaveChangesAsync();
        return true;
    }

    private static RecipeDetailResponse MapToDetailResponse(RecipeDetail d) => new()
    {
        Id = d.Id,
        IngredientId = d.IngredientId,
        IngredientName = d.Ingredient?.Name,
        ProductId = d.ProductId,
        ProductName = d.Product?.Name,
        Quantity = d.Quantity,
        Unit = d.Unit
    };
}

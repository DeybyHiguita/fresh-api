using Fresh.Core.DTOs.Ingredient;
using Fresh.Core.Entities;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fresh.Infrastructure.Services;

public class IngredientService : IIngredientService
{
    private readonly FreshDbContext _context;

    public IngredientService(FreshDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<IngredientResponse>> GetAllAsync()
    {
        var ingredients = await _context.Ingredients
            .Include(i => i.IngredientProducts)
                .ThenInclude(ip => ip.Product)
            .OrderBy(i => i.Name)
            .ToListAsync();

        return ingredients.Select(MapToResponse);
    }

    public async Task<IngredientResponse?> GetByIdAsync(int id)
    {
        var ingredient = await _context.Ingredients
            .Include(i => i.IngredientProducts)
                .ThenInclude(ip => ip.Product)
            .FirstOrDefaultAsync(i => i.Id == id);

        return ingredient == null ? null : MapToResponse(ingredient);
    }

    public async Task<IngredientResponse> CreateAsync(IngredientRequest request)
    {
        var normalizedName = request.Name.Trim().ToLower();
        var exists = await _context.Ingredients.AnyAsync(i => i.Name.ToLower() == normalizedName);

        if (exists)
            throw new InvalidOperationException($"Ya existe un ingrediente con el nombre '{request.Name.Trim()}'");

        var consumptions = await NormalizeConsumptionsAsync(request.Consumptions);

        var ingredient = new Ingredient
        {
            Name = request.Name.Trim(),
            Unit = request.Unit.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IngredientProducts = consumptions.Select(c => new IngredientProduct
            {
                ProductId = c.ProductId,
                Quantity = c.Quantity,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }).ToList(),
        };

        _context.Ingredients.Add(ingredient);
        await _context.SaveChangesAsync();

        return (await GetByIdAsync(ingredient.Id))!;
    }

    public async Task<IngredientResponse?> UpdateAsync(int id, IngredientRequest request)
    {
        var ingredient = await _context.Ingredients
            .Include(i => i.IngredientProducts)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (ingredient == null) return null;

        var normalizedName = request.Name.Trim().ToLower();
        var duplicateName = await _context.Ingredients
            .AnyAsync(i => i.Name.ToLower() == normalizedName && i.Id != id);

        if (duplicateName)
            throw new InvalidOperationException($"Ya existe un ingrediente con el nombre '{request.Name.Trim()}'");

        var consumptions = await NormalizeConsumptionsAsync(request.Consumptions);

        ingredient.Name = request.Name.Trim();
        ingredient.Unit = request.Unit.Trim();
        ingredient.UpdatedAt = DateTime.UtcNow;

        _context.IngredientProducts.RemoveRange(ingredient.IngredientProducts);
        ingredient.IngredientProducts = consumptions.Select(c => new IngredientProduct
        {
            IngredientId = ingredient.Id,
            ProductId = c.ProductId,
            Quantity = c.Quantity,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        }).ToList();

        await _context.SaveChangesAsync();

        return (await GetByIdAsync(id))!;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var ingredient = await _context.Ingredients
            .Include(i => i.RecipeIngredients)
            .Include(i => i.IngredientProducts)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (ingredient == null) return false;

        if (ingredient.RecipeIngredients.Any())
            throw new InvalidOperationException("No se puede eliminar el ingrediente porque está asociado a recetas");

        _context.IngredientProducts.RemoveRange(ingredient.IngredientProducts);
        _context.Ingredients.Remove(ingredient);
        await _context.SaveChangesAsync();

        return true;
    }

    private async Task<List<IngredientConsumptionRequest>> NormalizeConsumptionsAsync(List<IngredientConsumptionRequest> consumptions)
    {
        if (consumptions == null || consumptions.Count == 0)
            return new List<IngredientConsumptionRequest>();

        var grouped = consumptions
            .Where(c => c.ProductId > 0 && c.Quantity > 0)
            .GroupBy(c => c.ProductId)
            .Select(g => new IngredientConsumptionRequest
            {
                ProductId = g.Key,
                Quantity = g.Sum(x => x.Quantity),
            })
            .ToList();

        if (grouped.Count == 0)
            return grouped;

        var productIds = grouped.Select(c => c.ProductId).ToList();
        var existingIds = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync();

        var missingIds = productIds.Except(existingIds).ToList();
        if (missingIds.Count > 0)
            throw new InvalidOperationException($"Productos no encontrados: {string.Join(", ", missingIds)}");

        return grouped;
    }

    private static IngredientResponse MapToResponse(Ingredient ingredient) => new()
    {
        Id = ingredient.Id,
        Name = ingredient.Name,
        Unit = ingredient.Unit,
        Consumptions = ingredient.IngredientProducts
            .OrderBy(ip => ip.Product.Name)
            .Select(ip => new IngredientConsumptionResponse
            {
                Id = ip.Id,
                ProductId = ip.ProductId,
                ProductName = ip.Product.Name,
                ProductUnit = ip.Product.UnitMeasure,
                Quantity = ip.Quantity,
            })
            .ToList(),
        CreatedAt = ingredient.CreatedAt,
        UpdatedAt = ingredient.UpdatedAt,
    };
}

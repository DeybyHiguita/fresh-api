using Fresh.Core.DTOs.Category;
using Fresh.Core.Entities;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fresh.Infrastructure.Services;

public class CategoryService : ICategoryService
{
    private readonly FreshDbContext _context;

    public CategoryService(FreshDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CategoryResponse>> GetAllAsync()
    {
        var categories = await _context.Categories
            .OrderBy(c => c.Name)
            .ToListAsync();

        return categories.Select(MapToResponse);
    }

    public async Task<CategoryResponse?> GetByIdAsync(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        return category == null ? null : MapToResponse(category);
    }

    public async Task<CategoryResponse> CreateAsync(CategoryRequest request)
    {
        var normalizedName = request.Name.Trim().ToLower();
        var exists = await _context.Categories.AnyAsync(c => c.Name.ToLower() == normalizedName);

        if (exists)
            throw new InvalidOperationException($"Ya existe una categoría con el nombre '{request.Name.Trim()}'");

        var category = new Category
        {
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return MapToResponse(category);
    }

    public async Task<CategoryResponse?> UpdateAsync(int id, CategoryRequest request)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null) return null;

        var normalizedName = request.Name.Trim().ToLower();
        var duplicateName = await _context.Categories
            .AnyAsync(c => c.Name.ToLower() == normalizedName && c.Id != id);

        if (duplicateName)
            throw new InvalidOperationException($"Ya existe una categoría con el nombre '{request.Name.Trim()}'");

        category.Name = request.Name.Trim();
        category.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        category.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToResponse(category);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var category = await _context.Categories
            .Include(c => c.Recipes)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null) return false;

        if (category.Recipes.Any())
            throw new InvalidOperationException("No se puede eliminar la categoría porque tiene recetas asociadas");

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return true;
    }

    private static CategoryResponse MapToResponse(Category category) => new()
    {
        Id = category.Id,
        Name = category.Name,
        Description = category.Description,
        CreatedAt = category.CreatedAt,
        UpdatedAt = category.UpdatedAt,
    };
}

using Fresh.Core.DTOs.EquipmentCategory;
using Fresh.Core.Entities;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fresh.Infrastructure.Services;

public class EquipmentCategoryService : IEquipmentCategoryService
{
    private readonly FreshDbContext _context;

    public EquipmentCategoryService(FreshDbContext context) { _context = context; }

    public async Task<IEnumerable<EquipmentCategoryResponse>> GetAllAsync(bool onlyActive = true)
    {
        var query = _context.EquipmentCategories.AsQueryable();
        if (onlyActive) query = query.Where(c => c.IsActive);
        var categories = await query.OrderBy(c => c.Name).ToListAsync();
        return categories.Select(MapToResponse);
    }

    public async Task<EquipmentCategoryResponse?> GetByIdAsync(int id)
    {
        var category = await _context.EquipmentCategories.FindAsync(id);
        return category == null ? null : MapToResponse(category);
    }

    public async Task<EquipmentCategoryResponse> CreateAsync(EquipmentCategoryRequest request)
    {
        var exists = await _context.EquipmentCategories.AnyAsync(c => c.Name.ToLower() == request.Name.ToLower());
        if (exists) throw new InvalidOperationException($"La categoría '{request.Name}' ya existe.");

        var category = new EquipmentCategory
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = request.IsActive,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _context.EquipmentCategories.Add(category);
        await _context.SaveChangesAsync();
        return MapToResponse(category);
    }

    public async Task<EquipmentCategoryResponse?> UpdateAsync(int id, EquipmentCategoryRequest request)
    {
        var category = await _context.EquipmentCategories.FindAsync(id);
        if (category == null) return null;

        category.Name = request.Name;
        category.Description = request.Description;
        category.IsActive = request.IsActive;
        category.UpdatedAt = DateTimeOffset.UtcNow;

        _context.EquipmentCategories.Update(category);
        await _context.SaveChangesAsync();
        return MapToResponse(category);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var category = await _context.EquipmentCategories.FindAsync(id);
        if (category == null) return false;

        // Soft delete
        category.IsActive = false;
        category.UpdatedAt = DateTimeOffset.UtcNow;
        _context.EquipmentCategories.Update(category);
        await _context.SaveChangesAsync();
        return true;
    }

    private static EquipmentCategoryResponse MapToResponse(EquipmentCategory c) => new()
    {
        Id = c.Id, Name = c.Name, Description = c.Description, IsActive = c.IsActive
    };
}

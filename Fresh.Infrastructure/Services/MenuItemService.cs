using Fresh.Core.DTOs.MenuItem;
using Fresh.Core.Entities;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
namespace Fresh.Infrastructure.Services;

public class MenuItemService : IMenuItemService
{
    private readonly FreshDbContext _context;

    public MenuItemService(FreshDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<MenuItemResponse>> GetAllAsync()
    {
        var salesByItem = await _context.OrderItems
            .GroupBy(oi => oi.MenuItemId)
            .Select(g => new { MenuItemId = g.Key, TotalSold = g.Sum(oi => oi.Quantity) })
            .ToDictionaryAsync(x => x.MenuItemId, x => x.TotalSold);

        var menuItems = await _context.MenuItems
            .Include(m => m.Variants.OrderBy(v => v.SortOrder).ThenBy(v => v.VariantName))
            .ToListAsync();

        return menuItems
            .OrderByDescending(m => salesByItem.GetValueOrDefault(m.Id, 0))
            .ThenBy(m => m.SortOrder)
            .ThenBy(m => m.Name)
            .Select(MapToResponse);
    }

    public async Task<MenuItemResponse?> GetByIdAsync(int id)
    {
        var menuItem = await _context.MenuItems
            .Include(m => m.Variants.OrderBy(v => v.SortOrder).ThenBy(v => v.VariantName))
            .FirstOrDefaultAsync(m => m.Id == id);
        return menuItem != null ? MapToResponse(menuItem) : null;
    }

    public async Task<MenuItemResponse> CreateAsync(MenuItemRequest request)
    {
        var menuItem = new MenuItem
        {
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            PreparationCost = request.PreparationCost,
            SalePrice = request.SalePrice,
            IsAvailable = request.IsAvailable,
            ImgUrl = request.ImgUrl,
            SortOrder = request.SortOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.MenuItems.Add(menuItem);
        await _context.SaveChangesAsync();

        return MapToResponse(menuItem);
    }

    public async Task<MenuItemResponse?> UpdateAsync(int id, MenuItemRequest request)
    {
        var menuItem = await _context.MenuItems.FindAsync(id);
        if (menuItem == null) return null;

        menuItem.Name = request.Name;
        menuItem.Description = request.Description;
        menuItem.Category = request.Category;
        menuItem.PreparationCost = request.PreparationCost;
        menuItem.SalePrice = request.SalePrice;
        menuItem.IsAvailable = request.IsAvailable;
        menuItem.ImgUrl = request.ImgUrl;
        menuItem.SortOrder = request.SortOrder;
        menuItem.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToResponse(menuItem);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var menuItem = await _context.MenuItems.FindAsync(id);
        if (menuItem == null) return false;

        _context.MenuItems.Remove(menuItem);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task ReorderAsync(IEnumerable<ReorderMenuItemsRequest.SortOrderItem> items)
    {
        var updateList = items.ToList();
        var ids = updateList.Select(u => u.Id).ToHashSet();
        var menuItems = await _context.MenuItems.Where(m => ids.Contains(m.Id)).ToListAsync();
        foreach (var menuItem in menuItems)
        {
            var update = updateList.First(u => u.Id == menuItem.Id);
            menuItem.SortOrder = update.SortOrder;
            menuItem.UpdatedAt = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync();
    }

    // ── Variants ────────────────────────────────────────────────

    public async Task<MenuItemVariantResponse> AddVariantAsync(int menuItemId, MenuItemVariantRequest request)
    {
        var variant = new MenuItemVariant
        {
            MenuItemId = menuItemId,
            VariantName = request.VariantName,
            SalePrice = request.SalePrice,
            PreparationCost = request.PreparationCost,
            IsAvailable = request.IsAvailable,
            SortOrder = request.SortOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _context.MenuItemVariants.Add(variant);
        await _context.SaveChangesAsync();
        return MapVariant(variant);
    }

    public async Task<MenuItemVariantResponse?> UpdateVariantAsync(int menuItemId, int variantId, MenuItemVariantRequest request)
    {
        var variant = await _context.MenuItemVariants
            .FirstOrDefaultAsync(v => v.Id == variantId && v.MenuItemId == menuItemId);
        if (variant == null) return null;

        variant.VariantName = request.VariantName;
        variant.SalePrice = request.SalePrice;
        variant.PreparationCost = request.PreparationCost;
        variant.IsAvailable = request.IsAvailable;
        variant.SortOrder = request.SortOrder;
        variant.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return MapVariant(variant);
    }

    public async Task<bool> DeleteVariantAsync(int menuItemId, int variantId)
    {
        var variant = await _context.MenuItemVariants
            .FirstOrDefaultAsync(v => v.Id == variantId && v.MenuItemId == menuItemId);
        if (variant == null) return false;
        _context.MenuItemVariants.Remove(variant);
        await _context.SaveChangesAsync();
        return true;
    }

    private static MenuItemVariantResponse MapVariant(MenuItemVariant v) => new()
    {
        Id = v.Id,
        MenuItemId = v.MenuItemId,
        VariantName = v.VariantName,
        SalePrice = v.SalePrice,
        PreparationCost = v.PreparationCost,
        IsAvailable = v.IsAvailable,
        SortOrder = v.SortOrder,
    };

    private static MenuItemResponse MapToResponse(MenuItem menuItem) => new()
    {
        Id = menuItem.Id,
        Name = menuItem.Name,
        Description = menuItem.Description,
        Category = menuItem.Category,
        PreparationCost = menuItem.PreparationCost,
        SalePrice = menuItem.SalePrice,
        IsAvailable = menuItem.IsAvailable,
        ImgUrl = menuItem.ImgUrl,
        SortOrder = menuItem.SortOrder,
        CreatedAt = menuItem.CreatedAt,
        UpdatedAt = menuItem.UpdatedAt,
        Variants = menuItem.Variants.Select(v => new MenuItemVariantResponse
        {
            Id = v.Id,
            MenuItemId = v.MenuItemId,
            VariantName = v.VariantName,
            SalePrice = v.SalePrice,
            PreparationCost = v.PreparationCost,
            IsAvailable = v.IsAvailable,
            SortOrder = v.SortOrder,
        }).ToList()
    };
}

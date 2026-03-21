using Fresh.Core.DTOs.Product;
using Fresh.Core.Entities;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fresh.Infrastructure.Services;

public class ProductService : IProductService
{
    private readonly FreshDbContext _context;

    public ProductService(FreshDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ProductResponse>> GetAllAsync(bool onlyActive = true)
    {
        var query = _context.Products.AsQueryable();

        if (onlyActive)
            query = query.Where(p => p.IsActive);

        var products = await query
            .OrderBy(p => p.Name)
            .ToListAsync();

        return products.Select(MapToResponse);
    }

    public async Task<ProductResponse?> GetByIdAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        return product == null ? null : MapToResponse(product);
    }

    public async Task<ProductResponse> CreateAsync(ProductRequest request)
    {
        var exists = await _context.Products
            .AnyAsync(p => p.Name.ToLower() == request.Name.ToLower());

        if (exists)
            throw new InvalidOperationException($"Ya existe un producto con el nombre '{request.Name}'");

        var product = new Product
        {
            Name = request.Name,
            UnitMeasure = request.UnitMeasure,
            CurrentStock = request.CurrentStock,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return MapToResponse(product);
    }

    public async Task<ProductResponse?> UpdateAsync(int id, ProductRequest request)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return null;

        var duplicateName = await _context.Products
            .AnyAsync(p => p.Name.ToLower() == request.Name.ToLower() && p.Id != id);

        if (duplicateName)
            throw new InvalidOperationException($"Ya existe un producto con el nombre '{request.Name}'");

        product.Name = request.Name;
        product.UnitMeasure = request.UnitMeasure;
        product.CurrentStock = request.CurrentStock;
        product.IsActive = request.IsActive;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToResponse(product);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return false;

        // Soft delete: marcar como inactivo en lugar de eliminar físicamente
        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }

    private static ProductResponse MapToResponse(Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        UnitMeasure = product.UnitMeasure,
        CurrentStock = product.CurrentStock,
        IsActive = product.IsActive,
        CreatedAt = product.CreatedAt,
        UpdatedAt = product.UpdatedAt
    };
}

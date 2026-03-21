using Fresh.Core.DTOs.PurchaseBatch;
using Fresh.Core.DTOs.PurchaseDetail;
using Fresh.Core.Entities;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fresh.Infrastructure.Services;

public class PurchaseBatchService : IPurchaseBatchService
{
    private readonly FreshDbContext _context;

    public PurchaseBatchService(FreshDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<PurchaseBatchResponse>> GetAllAsync()
    {
        var batches = await _context.PurchaseBatches
            .Include(b => b.PurchaseDetails)
                .ThenInclude(d => d.Product)
            .OrderByDescending(b => b.StartDate)
            .ToListAsync();

        return batches.Select(MapToResponse);
    }

    public async Task<PurchaseBatchResponse?> GetByIdAsync(int id)
    {
        var batch = await _context.PurchaseBatches
            .Include(b => b.PurchaseDetails)
                .ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(b => b.Id == id);

        return batch == null ? null : MapToResponse(batch);
    }

    public async Task<PurchaseBatchResponse> CreateAsync(PurchaseBatchRequest request)
    {
        if (request.EndDate < request.StartDate)
            throw new ArgumentException("La fecha de fin no puede ser anterior a la fecha de inicio");

        var batch = new PurchaseBatch
        {
            BatchName = request.BatchName,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.PurchaseBatches.Add(batch);
        await _context.SaveChangesAsync();

        return MapToResponse(batch);
    }

    public async Task<PurchaseBatchResponse?> UpdateAsync(int id, PurchaseBatchRequest request)
    {
        var batch = await _context.PurchaseBatches
            .Include(b => b.PurchaseDetails)
                .ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (batch == null) return null;

        if (request.EndDate < request.StartDate)
            throw new ArgumentException("La fecha de fin no puede ser anterior a la fecha de inicio");

        batch.BatchName = request.BatchName;
        batch.StartDate = request.StartDate;
        batch.EndDate = request.EndDate;
        batch.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToResponse(batch);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var batch = await _context.PurchaseBatches.FindAsync(id);
        if (batch == null) return false;

        _context.PurchaseBatches.Remove(batch);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<PurchaseDetailResponse> AddDetailAsync(int batchId, PurchaseDetailRequest request)
    {
        var batchExists = await _context.PurchaseBatches.AnyAsync(b => b.Id == batchId);
        if (!batchExists)
            throw new KeyNotFoundException($"El lote con ID {batchId} no existe");

        var product = await _context.Products.FindAsync(request.ProductId);
        if (product == null)
            throw new KeyNotFoundException($"El producto con ID {request.ProductId} no existe");

        if (!product.IsActive)
            throw new InvalidOperationException($"El producto '{product.Name}' no está activo");

        var detail = new PurchaseDetail
        {
            BatchId = batchId,
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            TotalValue = request.TotalValue,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.PurchaseDetails.Add(detail);

        // Actualizar stock del producto
        product.CurrentStock += request.Quantity;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapDetailToResponse(detail, product);
    }

    public async Task<PurchaseDetailResponse?> UpdateDetailAsync(int batchId, int detailId, PurchaseDetailRequest request)
    {
        var detail = await _context.PurchaseDetails
            .Include(d => d.Product)
            .FirstOrDefaultAsync(d => d.Id == detailId && d.BatchId == batchId);

        if (detail == null) return null;

        var product = await _context.Products.FindAsync(request.ProductId);
        if (product == null)
            throw new KeyNotFoundException($"El producto con ID {request.ProductId} no existe");

        if (!product.IsActive)
            throw new InvalidOperationException($"El producto '{product.Name}' no está activo");

        // Revertir el stock anterior y aplicar el nuevo
        detail.Product.CurrentStock -= detail.Quantity;
        detail.Product.UpdatedAt = DateTime.UtcNow;

        product.CurrentStock += request.Quantity;
        product.UpdatedAt = DateTime.UtcNow;

        detail.ProductId = request.ProductId;
        detail.Quantity = request.Quantity;
        detail.TotalValue = request.TotalValue;
        detail.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapDetailToResponse(detail, product);
    }

    public async Task<bool> RemoveDetailAsync(int batchId, int detailId)
    {
        var detail = await _context.PurchaseDetails
            .Include(d => d.Product)
            .FirstOrDefaultAsync(d => d.Id == detailId && d.BatchId == batchId);

        if (detail == null) return false;

        // Revertir el stock
        detail.Product.CurrentStock -= detail.Quantity;
        detail.Product.UpdatedAt = DateTime.UtcNow;

        _context.PurchaseDetails.Remove(detail);
        await _context.SaveChangesAsync();

        return true;
    }

    // ?? Helpers ??????????????????????????????????????????????????????????????

    private static PurchaseBatchResponse MapToResponse(PurchaseBatch batch) => new()
    {
        Id = batch.Id,
        BatchName = batch.BatchName,
        StartDate = batch.StartDate,
        EndDate = batch.EndDate,
        CreatedAt = batch.CreatedAt,
        UpdatedAt = batch.UpdatedAt,
        Details = batch.PurchaseDetails
            .Select(d => MapDetailToResponse(d, d.Product))
            .ToList()
    };

    private static PurchaseDetailResponse MapDetailToResponse(PurchaseDetail detail, Product product) => new()
    {
        Id = detail.Id,
        BatchId = detail.BatchId,
        ProductId = detail.ProductId,
        ProductName = product.Name,
        ProductUnit = product.UnitMeasure,
        Quantity = detail.Quantity,
        TotalValue = detail.TotalValue,
        CreatedAt = detail.CreatedAt,
        UpdatedAt = detail.UpdatedAt
    };
}

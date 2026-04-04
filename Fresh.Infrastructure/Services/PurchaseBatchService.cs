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

    public async Task<(IEnumerable<PurchaseBatchResponse> Items, int Total)> GetPagedAsync(int skip, int take)
    {
        var query = _context.PurchaseBatches.OrderByDescending(b => b.StartDate);
        var total = await query.CountAsync();
        var batches = await query
            .Skip(skip)
            .Take(take)
            .Include(b => b.PurchaseDetails)
                .ThenInclude(d => d.Product)
            .ToListAsync();

        return (batches.Select(MapToResponse), total);
    }

    public async Task<(IEnumerable<PurchaseBatchSummary> Items, int Total)> GetSummariesAsync(int skip, int take, string? search)
    {
        var query = _context.PurchaseBatches.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(b => EF.Functions.ILike(b.BatchName, $"%{search.Trim()}%"));

        // Count and list run in parallel — one round-trip each, zero serialization
        var countTask = query.CountAsync();

        var itemsTask = query
            .OrderByDescending(b => b.StartDate)
            .Skip(skip)
            .Take(take)
            .Select(b => new PurchaseBatchSummary
            {
                Id        = b.Id,
                BatchName = b.BatchName,
                StartDate = b.StartDate,
                EndDate   = b.EndDate,
                Total     = b.PurchaseDetails.Sum(d => d.TotalValue),
            })
            .ToListAsync();

        await Task.WhenAll(countTask, itemsTask);

        return (itemsTask.Result, countTask.Result);
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
            throw new InvalidOperationException($"El producto '{product.Name}' no est� activo");

        var resolvedUnitPrice = request.UnitPrice > 0
            ? request.UnitPrice
            : (request.Quantity > 0 && request.TotalValue > 0 ? Math.Round(request.TotalValue / request.Quantity, 4) : 0m);

        var detail = new PurchaseDetail
        {
            BatchId = batchId,
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            TotalValue = request.TotalValue,
            UnitPrice = resolvedUnitPrice,
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
            throw new InvalidOperationException($"El producto '{product.Name}' no est� activo");

        // Revertir el stock anterior y aplicar el nuevo
        detail.Product.CurrentStock -= detail.Quantity;
        detail.Product.UpdatedAt = DateTime.UtcNow;

        product.CurrentStock += request.Quantity;
        product.UpdatedAt = DateTime.UtcNow;

        var resolvedUnitPrice = request.UnitPrice > 0
            ? request.UnitPrice
            : (request.Quantity > 0 && request.TotalValue > 0 ? Math.Round(request.TotalValue / request.Quantity, 4) : 0m);

        detail.ProductId = request.ProductId;
        detail.Quantity = request.Quantity;
        detail.TotalValue = request.TotalValue;
        detail.UnitPrice = resolvedUnitPrice;
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

    public async Task BatchUpdateDetailsAsync(List<BatchUpdateItem> updates)
    {
        var ids = updates.Select(u => u.Id).ToList();
        var details = await _context.PurchaseDetails
            .Where(d => ids.Contains(d.Id))
            .ToListAsync();

        foreach (var detail in details)
        {
            var update = updates.First(u => u.Id == detail.Id);
            detail.UnitPrice = update.UnitPrice;
            detail.TotalValue = update.TotalValue;
            detail.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<ProductPriceHistoryResponse>> GetProductPriceHistoryAsync(int productId)
    {
        return await _context.PurchaseDetails
            .Include(d => d.Batch)
            .Include(d => d.Product)
            .Where(d => d.ProductId == productId)
            .OrderByDescending(d => d.Batch.StartDate)
            .Select(d => new ProductPriceHistoryResponse
            {
                BatchId = d.BatchId,
                BatchName = d.Batch.BatchName,
                BatchDate = d.Batch.StartDate,
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice,
                TotalValue = d.TotalValue,
                ProductUnit = d.Product.UnitMeasure
            })
            .ToListAsync();
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
        UnitPrice = detail.UnitPrice,
        CreatedAt = detail.CreatedAt,
        UpdatedAt = detail.UpdatedAt
    };
}

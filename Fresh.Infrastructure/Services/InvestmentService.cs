using Fresh.Core.DTOs.Investment;
using Fresh.Core.Entities;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fresh.Infrastructure.Services;

public class InvestmentService : IInvestmentService
{
    private readonly FreshDbContext _context;

    public InvestmentService(FreshDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<InvestmentResponse>> GetAllAsync()
    {
        var investments = await _context.Investments
            .Include(i => i.Investor)
            .Include(i => i.Items)
                .ThenInclude(it => it.Equipment)
            .Include(i => i.Items)
                .ThenInclude(it => it.PurchaseBatch)
            .Include(i => i.Items)
                .ThenInclude(it => it.Product)
            .OrderByDescending(i => i.InvestmentDate)
            .ToListAsync();

        return investments.Select(MapToResponse);
    }

    public async Task<InvestmentResponse?> GetByIdAsync(int id)
    {
        var investment = await _context.Investments
            .Include(i => i.Investor)
            .Include(i => i.Items)
                .ThenInclude(it => it.Equipment)
            .Include(i => i.Items)
                .ThenInclude(it => it.PurchaseBatch)
            .Include(i => i.Items)
                .ThenInclude(it => it.Product)
            .FirstOrDefaultAsync(i => i.Id == id);

        return investment == null ? null : MapToResponse(investment);
    }

    public async Task<IEnumerable<InvestmentResponse>> GetByInvestorAsync(int investorId)
    {
        var investments = await _context.Investments
            .Include(i => i.Investor)
            .Include(i => i.Items)
                .ThenInclude(it => it.Equipment)
            .Include(i => i.Items)
                .ThenInclude(it => it.PurchaseBatch)
            .Include(i => i.Items)
                .ThenInclude(it => it.Product)
            .Where(i => i.InvestorId == investorId)
            .OrderByDescending(i => i.InvestmentDate)
            .ToListAsync();

        return investments.Select(MapToResponse);
    }

    public async Task<InvestmentResponse> CreateAsync(InvestmentRequest request)
    {
        // Validar que el inversionista exista
        var investor = await _context.Users.FindAsync(request.InvestorId);
        if (investor == null)
            throw new KeyNotFoundException($"El usuario con ID {request.InvestorId} no existe.");

        var investment = new Investment
        {
            InvestorId = request.InvestorId,
            Amount = request.Amount,
            InvestmentDate = request.InvestmentDate,
            Description = request.Description,
            Status = "Activo",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        _context.Investments.Add(investment);
        await _context.SaveChangesAsync();

        return (await GetByIdAsync(investment.Id))!;
    }

    public async Task<InvestmentResponse?> UpdateAsync(int id, InvestmentRequest request)
    {
        var investment = await _context.Investments.FindAsync(id);
        if (investment == null) return null;

        // Validar que el inversionista exista
        var investor = await _context.Users.FindAsync(request.InvestorId);
        if (investor == null)
            throw new KeyNotFoundException($"El usuario con ID {request.InvestorId} no existe.");

        investment.InvestorId = request.InvestorId;
        investment.Amount = request.Amount;
        investment.InvestmentDate = request.InvestmentDate;
        investment.Description = request.Description;
        if (!string.IsNullOrEmpty(request.Status))
            investment.Status = request.Status;
        investment.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var investment = await _context.Investments.FindAsync(id);
        if (investment == null) return false;

        _context.Investments.Remove(investment);
        await _context.SaveChangesAsync();
        return true;
    }

    // ── Items ────────────────────────────────────────────────────────────────

    public async Task<InvestmentItemResponse> AddItemAsync(InvestmentItemRequest request)
    {
        // Validar que la inversión exista
        var investment = await _context.Investments.FindAsync(request.InvestmentId);
        if (investment == null)
            throw new KeyNotFoundException($"La inversión con ID {request.InvestmentId} no existe.");

        // Validar referencias según el tipo
        await ValidateItemReferences(request);

        var item = new InvestmentItem
        {
            InvestmentId = request.InvestmentId,
            ItemType = request.ItemType,
            EquipmentId = request.EquipmentId,
            PurchaseBatchId = request.PurchaseBatchId,
            ProductId = request.ProductId,
            Description = request.Description,
            Amount = request.Amount,
            Quantity = request.Quantity,
            UnitPrice = request.UnitPrice,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _context.InvestmentItems.Add(item);
        investment.UpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();

        return MapItemToResponse(await LoadItemWithRelations(item.Id));
    }

    public async Task<InvestmentResponse> ImportNeedItemsAsync(int investmentId)
    {
        var investment = await _context.Investments.FindAsync(investmentId)
            ?? throw new KeyNotFoundException($"La inversión {investmentId} no existe.");

        // Buscar la solicitud vinculada a través de la asignación
        var assignment = await _context.InvestmentNeedAssignments
            .Include(a => a.Need).ThenInclude(n => n!.Items).ThenInclude(i => i.Equipment)
            .Include(a => a.Need).ThenInclude(n => n!.Items).ThenInclude(i => i.PurchaseBatch)
            .Include(a => a.Need).ThenInclude(n => n!.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(a => a.InvestmentId == investmentId);

        var need = assignment?.Need;
        if (need is null)
            throw new InvalidOperationException("No se encontró una solicitud de inversión vinculada.");

        var needItems = need.Items;
        if (needItems.Count == 0)
            throw new InvalidOperationException("La solicitud no tiene ítems para importar.");

        foreach (var ni in needItems)
        {
            var itemType = ni.ItemType ?? "other";
            var amount   = ni.EstimatedCost ?? (ni.Quantity.GetValueOrDefault(1) * ni.UnitPrice.GetValueOrDefault(0));

            _context.InvestmentItems.Add(new InvestmentItem
            {
                InvestmentId    = investmentId,
                ItemType        = itemType,
                EquipmentId     = ni.EquipmentId,
                PurchaseBatchId = ni.PurchaseBatchId,
                ProductId       = ni.ProductId,
                Description     = ni.Description,
                Amount          = amount,
                Quantity        = ni.Quantity,
                UnitPrice       = ni.UnitPrice,
                CreatedAt       = DateTimeOffset.UtcNow,
            });
        }

        investment.UpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();

        return await GetByIdAsync(investmentId) ?? throw new Exception("Error al recuperar la inversión.");
    }

    public async Task<bool> RemoveItemAsync(int itemId)
    {
        var item = await _context.InvestmentItems
            .Include(i => i.Investment)
            .FirstOrDefaultAsync(i => i.Id == itemId);
        
        if (item == null) return false;

        if (item.Investment != null)
            item.Investment.UpdatedAt = DateTimeOffset.UtcNow;

        _context.InvestmentItems.Remove(item);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<InvestmentItemResponse?> UpdateItemAsync(int itemId, InvestmentItemRequest request)
    {
        var item = await _context.InvestmentItems
            .Include(i => i.Investment)
            .FirstOrDefaultAsync(i => i.Id == itemId);
        
        if (item == null) return null;

        await ValidateItemReferences(request);

        item.ItemType = request.ItemType;
        item.EquipmentId = request.EquipmentId;
        item.PurchaseBatchId = request.PurchaseBatchId;
        item.ProductId = request.ProductId;
        item.Description = request.Description;
        item.Amount = request.Amount;
        item.Quantity = request.Quantity;
        item.UnitPrice = request.UnitPrice;

        if (item.Investment != null)
            item.Investment.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();
        return MapItemToResponse(await LoadItemWithRelations(itemId));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task ValidateItemReferences(InvestmentItemRequest request)
    {
        switch (request.ItemType.ToLower())
        {
            case "equipment":
                if (request.EquipmentId.HasValue)
                {
                    var equip = await _context.Equipments.FindAsync(request.EquipmentId.Value);
                    if (equip == null)
                        throw new KeyNotFoundException($"El equipo con ID {request.EquipmentId} no existe.");
                }
                break;

            case "purchase_batch":
                if (request.PurchaseBatchId.HasValue)
                {
                    var batch = await _context.PurchaseBatches.FindAsync(request.PurchaseBatchId.Value);
                    if (batch == null)
                        throw new KeyNotFoundException($"El lote de compra con ID {request.PurchaseBatchId} no existe.");
                }
                break;

            case "product":
                if (request.ProductId.HasValue)
                {
                    var product = await _context.Products.FindAsync(request.ProductId.Value);
                    if (product == null)
                        throw new KeyNotFoundException($"El producto con ID {request.ProductId} no existe.");
                }
                break;
        }
    }

    private async Task<InvestmentItem> LoadItemWithRelations(int itemId)
    {
        return await _context.InvestmentItems
            .Include(i => i.Equipment)
            .Include(i => i.PurchaseBatch)
            .Include(i => i.Product)
            .FirstAsync(i => i.Id == itemId);
    }

    private static InvestmentResponse MapToResponse(Investment i)
    {
        var justifiedAmount = i.Items.Sum(it => it.Amount);
        var pendingAmount = i.Amount - justifiedAmount;
        var percentage = i.Amount > 0 ? (double)(justifiedAmount / i.Amount * 100) : 0;

        return new InvestmentResponse
        {
            Id = i.Id,
            InvestorId = i.InvestorId,
            InvestorName = i.Investor?.Name ?? "Desconocido",
            Amount = i.Amount,
            JustifiedAmount = justifiedAmount,
            PendingAmount = pendingAmount,
            JustifiedPercentage = Math.Round(percentage, 2),
            InvestmentDate = i.InvestmentDate,
            Description = i.Description,
            Status = i.Status,
            CreatedAt = i.CreatedAt,
            UpdatedAt = i.UpdatedAt,
            Items = i.Items.Select(MapItemToResponse).ToList(),
        };
    }

    private static InvestmentItemResponse MapItemToResponse(InvestmentItem it)
    {
        var typeNames = new Dictionary<string, string>
        {
            { "equipment", "Equipo" },
            { "purchase_batch", "Lote de Compra" },
            { "product", "Producto" },
            { "other", "Otro" },
        };

        return new InvestmentItemResponse
        {
            Id = it.Id,
            InvestmentId = it.InvestmentId,
            ItemType = it.ItemType,
            ItemTypeName = typeNames.GetValueOrDefault(it.ItemType.ToLower(), "Otro"),
            EquipmentId = it.EquipmentId,
            EquipmentName = it.Equipment?.Name,
            PurchaseBatchId = it.PurchaseBatchId,
            PurchaseBatchName = it.PurchaseBatch?.BatchName,
            ProductId = it.ProductId,
            ProductName = it.Product?.Name,
            Description = it.Description,
            Amount = it.Amount,
            Quantity = it.Quantity,
            UnitPrice = it.UnitPrice,
            CreatedAt = it.CreatedAt,
        };
    }
}

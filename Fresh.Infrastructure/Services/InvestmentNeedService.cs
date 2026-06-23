using Fresh.Core.DTOs.Investment;
using Fresh.Core.Entities;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fresh.Infrastructure.Services;

public class InvestmentNeedService : IInvestmentNeedService
{
    private readonly FreshDbContext _context;

    public InvestmentNeedService(FreshDbContext context)
    {
        _context = context;
    }

    // ── Queries ──────────────────────────────────────────────────────────────

    public async Task<IEnumerable<InvestmentNeedResponse>> GetAllAsync()
    {
        var needs = await _context.InvestmentNeeds
            .Include(n => n.CreatedBy)
            .Include(n => n.Equipment)
            .Include(n => n.PurchaseBatch)
            .Include(n => n.Product)
            .Include(n => n.Assignments).ThenInclude(a => a.Investor)
            .Include(n => n.Items).ThenInclude(i => i.Equipment)
            .Include(n => n.Items).ThenInclude(i => i.PurchaseBatch)
            .Include(n => n.Items).ThenInclude(i => i.Product)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return needs.Select(MapToResponse);
    }

    public async Task<InvestmentNeedResponse?> GetByIdAsync(int id)
    {
        var need = await LoadNeedWithRelations(id);
        return need == null ? null : MapToResponse(need);
    }

    // ── Commands ─────────────────────────────────────────────────────────────

    public async Task<InvestmentNeedResponse> CreateAsync(InvestmentNeedRequest request)
    {
        var creator = await _context.Users.FindAsync(request.CreatedById);
        if (creator == null)
            throw new KeyNotFoundException($"El usuario con ID {request.CreatedById} no existe.");

        var need = new InvestmentNeed
        {
            Title       = request.Title,
            Description = request.Description,
            ItemType    = request.ItemType,
            EquipmentId = request.EquipmentId,
            PurchaseBatchId = request.PurchaseBatchId,
            ProductId   = request.ProductId,
            TotalNeeded = request.TotalNeeded,
            Status      = "Pendiente",
            CreatedById = request.CreatedById,
            CreatedAt   = DateTimeOffset.UtcNow,
            UpdatedAt   = DateTimeOffset.UtcNow,
        };

        _context.InvestmentNeeds.Add(need);
        await _context.SaveChangesAsync();

        foreach (var a in request.Assignments)
        {
            var investor = await _context.Users.FindAsync(a.InvestorId);
            if (investor == null)
                throw new KeyNotFoundException($"El inversionista con ID {a.InvestorId} no existe.");

            _context.InvestmentNeedAssignments.Add(new InvestmentNeedAssignment
            {
                NeedId          = need.Id,
                InvestorId      = a.InvestorId,
                SuggestedAmount = a.SuggestedAmount,
                Notes           = a.Notes,
                Status          = "Pendiente",
                CreatedAt       = DateTimeOffset.UtcNow,
                UpdatedAt       = DateTimeOffset.UtcNow,
            });
        }

        await _context.SaveChangesAsync();

        // Save items
        foreach (var item in request.Items)
        {
            _context.InvestmentNeedItems.Add(new InvestmentNeedItem
            {
                NeedId          = need.Id,
                ItemType        = item.ItemType,
                EquipmentId     = item.EquipmentId,
                PurchaseBatchId = item.PurchaseBatchId,
                ProductId       = item.ProductId,
                Description     = item.Description,
                Quantity        = item.Quantity,
                UnitPrice       = item.UnitPrice,
                EstimatedCost   = item.EstimatedCost,
                CreatedAt       = DateTimeOffset.UtcNow,
            });
        }

        await _context.SaveChangesAsync();

        return MapToResponse((await LoadNeedWithRelations(need.Id))!);
    }

    public async Task<InvestmentNeedResponse> UpdateAsync(int id, InvestmentNeedRequest request)
    {
        var need = await LoadNeedWithRelations(id);
        if (need == null)
            throw new KeyNotFoundException($"La solicitud con ID {id} no existe.");
        if (need.Status != "Pendiente")
            throw new InvalidOperationException("Solo se pueden editar solicitudes en estado Pendiente.");

        // Update scalar fields
        need.Title           = request.Title;
        need.Description     = request.Description;
        need.ItemType        = request.ItemType;
        need.EquipmentId     = request.EquipmentId;
        need.PurchaseBatchId = request.PurchaseBatchId;
        need.ProductId       = request.ProductId;
        need.TotalNeeded     = request.TotalNeeded;
        need.UpdatedAt       = DateTimeOffset.UtcNow;

        // Replace assignments (only pending ones)
        var existingAssignments = _context.InvestmentNeedAssignments
            .Where(a => a.NeedId == id && a.Status == "Pendiente");
        _context.InvestmentNeedAssignments.RemoveRange(existingAssignments);

        foreach (var a in request.Assignments)
        {
            _context.InvestmentNeedAssignments.Add(new InvestmentNeedAssignment
            {
                NeedId          = id,
                InvestorId      = a.InvestorId,
                SuggestedAmount = a.SuggestedAmount,
                Notes           = a.Notes,
                Status          = "Pendiente",
                CreatedAt       = DateTimeOffset.UtcNow,
                UpdatedAt       = DateTimeOffset.UtcNow,
            });
        }

        // Replace items
        var existingItems = _context.InvestmentNeedItems.Where(i => i.NeedId == id);
        _context.InvestmentNeedItems.RemoveRange(existingItems);

        foreach (var item in request.Items)
        {
            _context.InvestmentNeedItems.Add(new InvestmentNeedItem
            {
                NeedId          = id,
                ItemType        = item.ItemType,
                EquipmentId     = item.EquipmentId,
                PurchaseBatchId = item.PurchaseBatchId,
                ProductId       = item.ProductId,
                Description     = item.Description,
                Quantity        = item.Quantity,
                UnitPrice       = item.UnitPrice,
                EstimatedCost   = item.EstimatedCost,
                CreatedAt       = DateTimeOffset.UtcNow,
            });
        }

        await _context.SaveChangesAsync();
        return MapToResponse((await LoadNeedWithRelations(id))!);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var need = await _context.InvestmentNeeds.FindAsync(id);
        if (need == null) return false;
        if (need.Status == "Aprobada")
            throw new InvalidOperationException("No se puede eliminar una solicitud ya aprobada.");

        _context.InvestmentNeeds.Remove(need);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<InvestmentResponse>> ApproveAsync(int needId)
    {
        var need = await LoadNeedWithRelations(needId);
        if (need == null)
            throw new KeyNotFoundException($"La solicitud con ID {needId} no existe.");
        if (need.Status != "Pendiente")
            throw new InvalidOperationException("Solo se pueden aprobar solicitudes en estado Pendiente.");

        var createdInvestments = new List<Investment>();
        var today = DateOnly.FromDateTime(DateTime.Today);

        foreach (var assignment in need.Assignments.Where(a => a.Status == "Pendiente"))
        {
            var investment = new Investment
            {
                InvestorId      = assignment.InvestorId,
                Amount          = assignment.SuggestedAmount,
                InvestmentDate  = today,
                Description     = $"(Solicitud) {need.Title}",
                Status          = "Pendiente",
                CreatedAt       = DateTimeOffset.UtcNow,
                UpdatedAt       = DateTimeOffset.UtcNow,
            };

            _context.Investments.Add(investment);
            await _context.SaveChangesAsync();

            assignment.InvestmentId = investment.Id;
            assignment.Status       = "Aprobado";
            assignment.UpdatedAt    = DateTimeOffset.UtcNow;
            createdInvestments.Add(investment);
        }

        need.Status    = "Aprobada";
        need.UpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();

        // Reload each investment with full relations for response mapping
        var investmentService = new InvestmentService(_context);
        var responses = new List<InvestmentResponse>();
        foreach (var inv in createdInvestments)
        {
            var full = await investmentService.GetByIdAsync(inv.Id);
            if (full != null) responses.Add(full);
        }
        return responses;
    }

    public async Task<bool> RejectAsync(int needId)
    {
        var need = await _context.InvestmentNeeds.FindAsync(needId);
        if (need == null) return false;
        if (need.Status != "Pendiente")
            throw new InvalidOperationException("Solo se pueden rechazar solicitudes en estado Pendiente.");

        need.Status    = "Rechazada";
        need.UpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<InvestmentNeed?> LoadNeedWithRelations(int id)
    {
        return await _context.InvestmentNeeds
            .Include(n => n.CreatedBy)
            .Include(n => n.Equipment)
            .Include(n => n.PurchaseBatch)
            .Include(n => n.Product)
            .Include(n => n.Assignments).ThenInclude(a => a.Investor)
            .Include(n => n.Items).ThenInclude(i => i.Equipment)
            .Include(n => n.Items).ThenInclude(i => i.PurchaseBatch)
            .Include(n => n.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(n => n.Id == id);
    }

    private static InvestmentNeedResponse MapToResponse(InvestmentNeed n)
    {
        return new InvestmentNeedResponse
        {
            Id                = n.Id,
            Title             = n.Title,
            Description       = n.Description,
            ItemType          = n.ItemType,
            EquipmentId       = n.EquipmentId,
            EquipmentName     = n.Equipment?.Name,
            PurchaseBatchId   = n.PurchaseBatchId,
            PurchaseBatchName = n.PurchaseBatch?.BatchName,
            ProductId         = n.ProductId,
            ProductName       = n.Product?.Name,
            TotalNeeded       = n.TotalNeeded,
            AssignedTotal     = n.Assignments.Sum(a => a.SuggestedAmount),
            Status            = n.Status,
            CreatedById       = n.CreatedById,
            CreatedByName     = n.CreatedBy?.Name ?? "Desconocido",
            CreatedAt         = n.CreatedAt,
            Assignments       = n.Assignments.Select(a => new InvestmentNeedAssignmentResponse
            {
                Id              = a.Id,
                NeedId          = a.NeedId,
                InvestorId      = a.InvestorId,
                InvestorName    = a.Investor?.Name ?? "Desconocido",
                SuggestedAmount = a.SuggestedAmount,
                Status          = a.Status,
                Notes           = a.Notes,
                InvestmentId    = a.InvestmentId,
            }).ToList(),
            Items = n.Items.Select(i => new InvestmentNeedItemResponse
            {
                Id                = i.Id,
                NeedId            = i.NeedId,
                ItemType          = i.ItemType,
                EquipmentId       = i.EquipmentId,
                EquipmentName     = i.Equipment?.Name,
                PurchaseBatchId   = i.PurchaseBatchId,
                PurchaseBatchName = i.PurchaseBatch?.BatchName,
                ProductId         = i.ProductId,
                ProductName       = i.Product?.Name,
                Description       = i.Description,
                Quantity          = i.Quantity,
                UnitPrice         = i.UnitPrice,
                EstimatedCost     = i.EstimatedCost,
            }).ToList(),
        };
    }
}

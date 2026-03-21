using Fresh.Core.DTOs.ExpenseType;
using Fresh.Core.Entities;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fresh.Infrastructure.Services;

public class ExpenseTypeService : IExpenseTypeService
{
    private readonly FreshDbContext _context;

    public ExpenseTypeService(FreshDbContext context) { _context = context; }

    public async Task<IEnumerable<ExpenseTypeResponse>> GetAllAsync(bool onlyActive = true)
    {
        var query = _context.ExpenseTypes.AsQueryable();
        if (onlyActive) query = query.Where(e => e.IsActive);
        var types = await query.OrderBy(e => e.Name).ToListAsync();
        return types.Select(MapToResponse);
    }

    public async Task<ExpenseTypeResponse?> GetByIdAsync(int id)
    {
        var type = await _context.ExpenseTypes.FindAsync(id);
        return type == null ? null : MapToResponse(type);
    }

    public async Task<ExpenseTypeResponse> CreateAsync(ExpenseTypeRequest request)
    {
        var exists = await _context.ExpenseTypes.AnyAsync(e => e.Name.ToLower() == request.Name.ToLower());
        if (exists) throw new InvalidOperationException($"Ya existe un tipo de gasto llamado '{request.Name}'");

        var type = new ExpenseType
        {
            Name = request.Name,
            Description = request.Description,
            ExpectedAmount = request.ExpectedAmount,
            Frequency = request.Frequency,
            IsActive = request.IsActive,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _context.ExpenseTypes.Add(type);
        await _context.SaveChangesAsync();
        return MapToResponse(type);
    }

    public async Task<ExpenseTypeResponse?> UpdateAsync(int id, ExpenseTypeRequest request)
    {
        var type = await _context.ExpenseTypes.FindAsync(id);
        if (type == null) return null;

        type.Name = request.Name;
        type.Description = request.Description;
        type.ExpectedAmount = request.ExpectedAmount;
        type.Frequency = request.Frequency;
        type.IsActive = request.IsActive;
        type.UpdatedAt = DateTimeOffset.UtcNow;

        _context.ExpenseTypes.Update(type);
        await _context.SaveChangesAsync();
        return MapToResponse(type);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var type = await _context.ExpenseTypes.FindAsync(id);
        if (type == null) return false;

        type.IsActive = false; // Soft delete
        type.UpdatedAt = DateTimeOffset.UtcNow;
        _context.ExpenseTypes.Update(type);
        await _context.SaveChangesAsync();
        return true;
    }

    private static ExpenseTypeResponse MapToResponse(ExpenseType e) => new()
    {
        Id = e.Id, Name = e.Name, Description = e.Description,
        ExpectedAmount = e.ExpectedAmount, Frequency = e.Frequency, IsActive = e.IsActive
    };
}
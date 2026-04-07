using Fresh.Core.DTOs.Safe;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fresh.Infrastructure.Services;

public class SafeService : ISafeService
{
    private readonly FreshDbContext _db;
    public SafeService(FreshDbContext db) { _db = db; }

    public async Task<SafeResponse> GetSafeAsync()
    {
        var safe = await _db.Safes.FirstOrDefaultAsync();
        if (safe == null)
        {
            safe = new Core.Entities.Safe();
            _db.Safes.Add(safe);
            await _db.SaveChangesAsync();
        }
        return new SafeResponse { Id = safe.Id, Balance = safe.Balance, UpdatedAt = safe.UpdatedAt };
    }

    public async Task<IEnumerable<SafeTransactionResponse>> GetTransactionsAsync(int? limit = null)
    {
        var q = _db.SafeTransactions
            .Include(t => t.CreatedBy)
            .OrderByDescending(t => t.CreatedAt)
            .AsQueryable();

        if (limit.HasValue)
            q = q.Take(limit.Value);

        return await q.Select(t => new SafeTransactionResponse
        {
            Id             = t.Id,
            Type           = t.Type,
            Amount         = t.Amount,
            Description    = t.Description,
            BalanceBefore  = t.BalanceBefore,
            BalanceAfter   = t.BalanceAfter,
            CashRegisterId = t.CashRegisterId,
            CreatedByName  = t.CreatedBy != null ? t.CreatedBy.Name : null,
            CreatedAt      = t.CreatedAt,
        }).ToListAsync();
    }

    public async Task<SafeTransactionResponse> AddExpenseAsync(SafeExpenseRequest request)
    {
        var safe = await _db.Safes.FirstOrDefaultAsync()
                   ?? throw new InvalidOperationException("Caja fuerte no inicializada.");

        if (request.Amount <= 0)
            throw new ArgumentException("El monto debe ser mayor a cero.");

        if (safe.Balance < request.Amount)
            throw new InvalidOperationException("Saldo insuficiente en caja fuerte.");

        var before = safe.Balance;
        safe.Balance  -= request.Amount;
        safe.UpdatedAt = DateTimeOffset.UtcNow;

        var tx = new Core.Entities.SafeTransaction
        {
            Type          = "Gasto",
            Amount        = request.Amount,
            Description   = request.Description,
            BalanceBefore = before,
            BalanceAfter  = safe.Balance,
            CreatedById   = request.CreatedById,
        };
        _db.SafeTransactions.Add(tx);
        await _db.SaveChangesAsync();

        return await GetTxResponse(tx.Id);
    }

    public async Task<SafeTransactionResponse> AddDepositAsync(SafeDepositRequest request)
    {
        var safe = await _db.Safes.FirstOrDefaultAsync()
                   ?? throw new InvalidOperationException("Caja fuerte no inicializada.");

        if (request.Amount <= 0)
            throw new ArgumentException("El monto debe ser mayor a cero.");

        var before = safe.Balance;
        safe.Balance  += request.Amount;
        safe.UpdatedAt = DateTimeOffset.UtcNow;

        var tx = new Core.Entities.SafeTransaction
        {
            Type            = "Ingreso",
            Amount          = request.Amount,
            Description     = request.Description,
            BalanceBefore   = before,
            BalanceAfter    = safe.Balance,
            CashRegisterId  = request.CashRegisterId,
            CreatedById     = request.CreatedById,
        };
        _db.SafeTransactions.Add(tx);
        await _db.SaveChangesAsync();

        return await GetTxResponse(tx.Id);
    }

    private async Task<SafeTransactionResponse> GetTxResponse(int id)
    {
        return await _db.SafeTransactions
            .Include(t => t.CreatedBy)
            .Where(t => t.Id == id)
            .Select(t => new SafeTransactionResponse
            {
                Id             = t.Id,
                Type           = t.Type,
                Amount         = t.Amount,
                Description    = t.Description,
                BalanceBefore  = t.BalanceBefore,
                BalanceAfter   = t.BalanceAfter,
                CashRegisterId = t.CashRegisterId,
                CreatedByName  = t.CreatedBy != null ? t.CreatedBy.Name : null,
                CreatedAt      = t.CreatedAt,
            }).FirstAsync();
    }
}

using Fresh.Core.DTOs.Safe;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fresh.Infrastructure.Services;

public class SafeService : ISafeService
{
    private readonly FreshDbContext _db;
    public SafeService(FreshDbContext db) { _db = db; }

    public async Task<SafeResponse> GetSafeAsync(string safeType = "caja_fuerte")
    {
        var safe = await _db.Safes.FirstOrDefaultAsync(s => s.SafeType == safeType);
        if (safe == null)
        {
            safe = new Core.Entities.Safe { SafeType = safeType };
            _db.Safes.Add(safe);
            await _db.SaveChangesAsync();
        }
        return new SafeResponse { Id = safe.Id, Balance = safe.Balance, SafeType = safe.SafeType, UpdatedAt = safe.UpdatedAt };
    }

    public async Task<IEnumerable<SafeTransactionResponse>> GetTransactionsAsync(string safeType = "caja_fuerte", int? limit = null)
    {
        var q = _db.SafeTransactions
            .Include(t => t.CreatedBy)
            .Include(t => t.PurchaseBatch)
            .Include(t => t.Expense).ThenInclude(e => e!.ExpenseType)
            .Where(t => t.SafeType == safeType)
            .OrderByDescending(t => t.CreatedAt)
            .AsQueryable();

        if (limit.HasValue)
            q = q.Take(limit.Value);

        return await q.Select(t => new SafeTransactionResponse
        {
            Id                = t.Id,
            Type              = t.Type,
            Amount            = t.Amount,
            Description       = t.Description,
            BalanceBefore     = t.BalanceBefore,
            BalanceAfter      = t.BalanceAfter,
            CashRegisterId    = t.CashRegisterId,
            CreatedByName     = t.CreatedBy != null ? t.CreatedBy.Name : null,
            PurchaseBatchId   = t.PurchaseBatchId,
            PurchaseBatchName = t.PurchaseBatch != null ? t.PurchaseBatch.BatchName : null,
            ExpenseId         = t.ExpenseId,
            ExpenseTypeName   = t.Expense != null && t.Expense.ExpenseType != null ? t.Expense.ExpenseType.Name : null,
            CreatedAt         = t.CreatedAt,
        }).ToListAsync();
    }

    public async Task<SafeTransactionResponse> AddExpenseAsync(SafeExpenseRequest request)
    {
        var safeType = request.SafeType ?? "caja_fuerte";
        var safe = await _db.Safes.FirstOrDefaultAsync(s => s.SafeType == safeType)
                   ?? throw new InvalidOperationException("Caja fuerte no inicializada.");

        if (request.Amount <= 0)
            throw new ArgumentException("El monto debe ser mayor a cero.");

        if (safe.Balance < request.Amount)
            throw new InvalidOperationException("Saldo insuficiente.");

        var before = safe.Balance;
        safe.Balance  -= request.Amount;
        safe.UpdatedAt = DateTimeOffset.UtcNow;

        var tx = new Core.Entities.SafeTransaction
        {
            Type            = "Gasto",
            Amount          = request.Amount,
            Description     = request.Description,
            BalanceBefore   = before,
            BalanceAfter    = safe.Balance,
            CreatedById     = request.CreatedById,
            SafeType        = safeType,
            PurchaseBatchId = request.PurchaseBatchId,
            ExpenseId       = request.ExpenseId,
        };
        _db.SafeTransactions.Add(tx);
        await _db.SaveChangesAsync();

        return await GetTxResponse(tx.Id);
    }

    public async Task<SafeTransactionResponse> AddDepositAsync(SafeDepositRequest request)
    {
        var safeType = request.SafeType ?? "caja_fuerte";
        var safe = await _db.Safes.FirstOrDefaultAsync(s => s.SafeType == safeType)
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
            SafeType        = safeType,
        };
        _db.SafeTransactions.Add(tx);
        await _db.SaveChangesAsync();

        return await GetTxResponse(tx.Id);
    }

    private async Task<SafeTransactionResponse> GetTxResponse(int id)
    {
        return await _db.SafeTransactions
            .Include(t => t.CreatedBy)
            .Include(t => t.PurchaseBatch)
            .Include(t => t.Expense).ThenInclude(e => e!.ExpenseType)
            .Where(t => t.Id == id)
            .Select(t => new SafeTransactionResponse
            {
                Id                = t.Id,
                Type              = t.Type,
                Amount            = t.Amount,
                Description       = t.Description,
                BalanceBefore     = t.BalanceBefore,
                BalanceAfter      = t.BalanceAfter,
                CashRegisterId    = t.CashRegisterId,
                CreatedByName     = t.CreatedBy != null ? t.CreatedBy.Name : null,
                PurchaseBatchId   = t.PurchaseBatchId,
                PurchaseBatchName = t.PurchaseBatch != null ? t.PurchaseBatch.BatchName : null,
                ExpenseId         = t.ExpenseId,
                ExpenseTypeName   = t.Expense != null && t.Expense.ExpenseType != null ? t.Expense.ExpenseType.Name : null,
                CreatedAt         = t.CreatedAt,
            }).FirstAsync();
    }
}

using Fresh.Core.DTOs.CashPeriod;
using Fresh.Core.Entities;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fresh.Infrastructure.Services;

public class CashPeriodService : ICashPeriodService
{
    private readonly FreshDbContext _context;

    public CashPeriodService(FreshDbContext context) { _context = context; }

    public async Task<IEnumerable<CashPeriodResponse>> GetAllAsync()
    {
        var periods = await _context.CashPeriods.OrderByDescending(p => p.StartDate).ToListAsync();
        var result = new List<CashPeriodResponse>();
        foreach (var p in periods)
            result.Add(await BuildResponseAsync(p));
        return result;
    }

    public async Task<CashPeriodResponse?> GetByIdAsync(int id)
    {
        var period = await _context.CashPeriods.FindAsync(id);
        return period == null ? null : await BuildResponseAsync(period);
    }

    public async Task<CashPeriodResponse> CreateAsync(CashPeriodRequest request)
    {
        if (request.EndDate < request.StartDate)
            throw new ArgumentException("La fecha de fin no puede ser menor a la de inicio.");

        var period = new CashPeriod
        {
            Name = request.Name,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsClosed = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _context.CashPeriods.Add(period);
        await _context.SaveChangesAsync();
        return await BuildResponseAsync(period);
    }

    public async Task<CashPeriodResponse?> ClosePeriodAsync(int id)
    {
        var period = await _context.CashPeriods
            .Include(p => p.CashRegisters)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (period == null) return null;

        if (period.CashRegisters.Any(cr => cr.Status == "Abierta"))
            throw new InvalidOperationException("No se puede cerrar el periodo. Hay cajas abiertas.");

        period.IsClosed = true;
        period.UpdatedAt = DateTimeOffset.UtcNow;
        _context.CashPeriods.Update(period);
        await _context.SaveChangesAsync();
        return await BuildResponseAsync(period);
    }

    private async Task<CashPeriodResponse> BuildResponseAsync(CashPeriod p)
    {
        var expenses = (await _context.Expenses.ToListAsync())
            .Where(e => e.PaymentDate >= p.StartDate && e.PaymentDate <= p.EndDate)
            .ToList();

        decimal expCash     = expenses.Where(e => e.PaymentMethod.Equals("Efectivo",      StringComparison.OrdinalIgnoreCase)).Sum(e => e.AmountPaid);
        decimal expTransfer = expenses.Where(e => e.PaymentMethod.Equals("Transferencia", StringComparison.OrdinalIgnoreCase)).Sum(e => e.AmountPaid);
        decimal expCard     = expenses.Where(e => e.PaymentMethod.Equals("Tarjeta",       StringComparison.OrdinalIgnoreCase)).Sum(e => e.AmountPaid);

        return new CashPeriodResponse
        {
            Id               = p.Id,
            Name             = p.Name,
            StartDate        = p.StartDate,
            EndDate          = p.EndDate,
            IsClosed         = p.IsClosed,
            TotalExpenses    = expCash + expTransfer + expCard,
            ExpensesCash     = expCash,
            ExpensesTransfer = expTransfer,
            ExpensesCard     = expCard,
            ExpenseCount     = expenses.Count,
        };
    }
}


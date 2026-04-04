using Fresh.Core.DTOs.Expense;
using Fresh.Core.Entities;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fresh.Infrastructure.Services;

public class ExpenseService : IExpenseService
{
    private readonly FreshDbContext _context;

    public ExpenseService(FreshDbContext context) { _context = context; }

    public async Task<IEnumerable<ExpenseResponse>> GetAllAsync()
    {
        var expenses = await _context.Expenses
            .Include(e => e.ExpenseType)
            .Include(e => e.User)
            .Include(e => e.PurchaseBatch)
            .OrderByDescending(e => e.PaymentDate)
            .ToListAsync();
        return expenses.Select(MapToResponse);
    }

    public async Task<ExpenseResponse?> GetByIdAsync(int id)
    {
        var expense = await _context.Expenses
            .Include(e => e.ExpenseType)
            .Include(e => e.User)
            .Include(e => e.PurchaseBatch)
            .FirstOrDefaultAsync(e => e.Id == id);
        return expense == null ? null : MapToResponse(expense);
    }

    public async Task<ExpenseResponse> CreateAsync(ExpenseRequest request)
    {
        var typeExists = await _context.ExpenseTypes.AnyAsync(t => t.Id == request.ExpenseTypeId);
        if (!typeExists) throw new KeyNotFoundException("El tipo de gasto seleccionado no existe.");

        var userExists = await _context.Users.AnyAsync(u => u.Id == request.UserId);
        if (!userExists) throw new KeyNotFoundException("El usuario seleccionado no existe.");

        var expense = new Expense
        {
            ExpenseTypeId   = request.ExpenseTypeId,
            UserId          = request.UserId,
            AmountPaid      = request.AmountPaid,
            PaymentDate     = request.PaymentDate,
            PaymentMethod   = request.PaymentMethod,
            Notes           = request.Notes,
            PurchaseBatchId = request.PurchaseBatchId,
            CreatedAt       = DateTimeOffset.UtcNow,
            UpdatedAt       = DateTimeOffset.UtcNow
        };

        _context.Expenses.Add(expense);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(expense.Id) ?? throw new Exception("Error al mapear el gasto.");
    }

    // Implementaci�n agregada para cumplir con la interfaz
    public async Task<IEnumerable<ExpenseResponse>> GetByMonthYearAsync(int month, int year)
    {
        var expenses = await _context.Expenses
            .Include(e => e.ExpenseType)
            .Include(e => e.User)
            .Include(e => e.PurchaseBatch)
            .Where(e => e.PaymentDate.Month == month && e.PaymentDate.Year == year)
            .OrderByDescending(e => e.PaymentDate)
            .ToListAsync();

        return expenses.Select(MapToResponse);
    }

    public async Task<ExpenseResponse?> UpdateAsync(int id, ExpenseRequest request)
    {
        var expense = await _context.Expenses
            .FirstOrDefaultAsync(e => e.Id == id);

        if (expense == null)
            return null;

        var typeExists = await _context.ExpenseTypes.AnyAsync(t => t.Id == request.ExpenseTypeId);
        if (!typeExists) throw new KeyNotFoundException("El tipo de gasto seleccionado no existe.");

        var userExists = await _context.Users.AnyAsync(u => u.Id == request.UserId);
        if (!userExists) throw new KeyNotFoundException("El usuario seleccionado no existe.");

        expense.ExpenseTypeId   = request.ExpenseTypeId;
        expense.UserId          = request.UserId;
        expense.AmountPaid      = request.AmountPaid;
        expense.PaymentDate     = request.PaymentDate;
        expense.PaymentMethod   = request.PaymentMethod;
        expense.Notes           = request.Notes;
        expense.PurchaseBatchId = request.PurchaseBatchId;
        expense.UpdatedAt       = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(expense.Id);
    }

    private static ExpenseResponse MapToResponse(Expense e) => new()
    {
        Id                = e.Id,
        ExpenseTypeId     = e.ExpenseTypeId,
        ExpenseTypeName   = e.ExpenseType?.Name ?? "Desconocido",
        UserId            = e.UserId,
        UserName          = e.User?.Name ?? "Desconocido",
        AmountPaid        = e.AmountPaid,
        PaymentDate       = e.PaymentDate,
        PaymentMethod     = e.PaymentMethod,
        Notes             = e.Notes,
        PurchaseBatchId   = e.PurchaseBatchId,
        PurchaseBatchName = e.PurchaseBatch?.BatchName,
        CreatedAt         = e.CreatedAt
    };
}
using Fresh.Core.DTOs.CashRegister;
using Fresh.Core.Entities;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fresh.Infrastructure.Services;

public class CashRegisterService : ICashRegisterService
{
    private readonly FreshDbContext _context;

    public CashRegisterService(FreshDbContext context) { _context = context; }

    public async Task<IEnumerable<CashRegisterResponse>> GetAllAsync(int? periodId = null)
    {
        var query = _context.CashRegisters
            .Include(c => c.Period)
            .Include(c => c.OpenedBy)
            .Include(c => c.ClosedBy)
            .AsQueryable();

        if (periodId.HasValue) query = query.Where(c => c.PeriodId == periodId);

        var registers = await query.OrderByDescending(c => c.OpeningTime).ToListAsync();
        return registers.Select(MapToResponse);
    }

    public async Task<CashRegisterResponse?> GetByIdAsync(int id)
    {
        var register = await _context.CashRegisters
            .Include(c => c.Period)
            .Include(c => c.OpenedBy)
            .Include(c => c.ClosedBy)
            .FirstOrDefaultAsync(c => c.Id == id);
        return register == null ? null : MapToResponse(register);
    }

    public async Task<CashRegisterResponse> OpenRegisterAsync(OpenCashRegisterRequest request)
    {
        var period = await _context.CashPeriods.FindAsync(request.PeriodId);
        if (period == null || period.IsClosed)
            throw new InvalidOperationException("El periodo no existe o está cerrado.");

        var openRegister = await _context.CashRegisters.AnyAsync(c => c.Status == "Abierta");
        if (openRegister)
            throw new InvalidOperationException("Ya existe una caja abierta. Debe cerrarla primero.");

        var register = new CashRegister
        {
            PeriodId = request.PeriodId,
            OpenedById = request.OpenedById,
            OpeningBalance = request.OpeningBalance,
            OpeningTime = DateTimeOffset.UtcNow,
            Status = "Abierta",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _context.CashRegisters.Add(register);
        await _context.SaveChangesAsync();
        return await GetByIdAsync(register.Id) ?? throw new Exception("Error al mapear caja.");
    }

    public async Task<CashSystemTotalsResponse?> GetSystemTotalsAsync(int id)
    {
        var register = await _context.CashRegisters.FindAsync(id);
        if (register == null) return null;

        var until = register.ClosingTime ?? DateTimeOffset.UtcNow;
        var invoices = await _context.Invoices
            .Where(i => i.CreatedAt >= register.OpeningTime && i.CreatedAt <= until)
            .ToListAsync();

        decimal systemCash = register.OpeningBalance
            + invoices
                .Where(i => i.PaymentMethod.Equals("Efectivo", StringComparison.OrdinalIgnoreCase))
                .Sum(i => i.TotalAmount);

        decimal systemTransfer = invoices
            .Where(i => i.PaymentMethod.Equals("Transferencia", StringComparison.OrdinalIgnoreCase))
            .Sum(i => i.TotalAmount);

        decimal systemCard = invoices
            .Where(i => i.PaymentMethod.Equals("Tarjeta", StringComparison.OrdinalIgnoreCase))
            .Sum(i => i.TotalAmount);

        // Gastos del turno: se filtra por PaymentDate (fecha local Colombia UTC-5) para evitar
        // problemas de zona horaria con CreatedAt UTC, y para respetar la fecha que el usuario registró.
        var colombiaOffset = TimeSpan.FromHours(-5);
        var openDateLocal  = DateOnly.FromDateTime(register.OpeningTime.ToOffset(colombiaOffset).DateTime);
        var closeDateLocal = DateOnly.FromDateTime(until.ToOffset(colombiaOffset).DateTime);

        var expenses = await _context.Expenses
            .Where(e => e.PaymentDate >= openDateLocal && e.PaymentDate <= closeDateLocal)
            .ToListAsync();

        decimal expCash     = expenses.Where(e => e.PaymentMethod.Equals("Efectivo",      StringComparison.OrdinalIgnoreCase)).Sum(e => e.AmountPaid);
        decimal expTransfer = expenses.Where(e => e.PaymentMethod.Equals("Transferencia", StringComparison.OrdinalIgnoreCase)).Sum(e => e.AmountPaid);
        decimal expCard     = expenses.Where(e => e.PaymentMethod.Equals("Tarjeta",       StringComparison.OrdinalIgnoreCase)).Sum(e => e.AmountPaid);
        decimal totalExp    = expCash + expTransfer + expCard;

        return new CashSystemTotalsResponse
        {
            RegisterId       = id,
            OpeningBalance   = register.OpeningBalance,
            SystemCash       = systemCash,
            SystemTransfer   = systemTransfer,
            SystemCard       = systemCard,
            TotalInvoices    = systemCash - register.OpeningBalance + systemTransfer + systemCard,
            InvoiceCount     = invoices.Count,
            ExpensesCash     = expCash,
            ExpensesTransfer = expTransfer,
            ExpensesCard     = expCard,
            TotalExpenses    = totalExp,
            ExpenseCount     = expenses.Count,
            NetCash          = systemCash - expCash,
            NetTransfer      = systemTransfer - expTransfer,
            NetCard          = systemCard - expCard,
        };
    }

    public async Task<CashRegisterResponse?> CloseRegisterAsync(int id, CloseCashRegisterRequest request)
    {
        var register = await _context.CashRegisters.FindAsync(id);
        if (register == null) return null;
        if (register.Status != "Abierta")
            throw new InvalidOperationException("Esta caja ya fue cerrada.");

        var closingTime = DateTimeOffset.UtcNow;

        // Calcular totales del sistema desde facturas del turno
        var invoices = await _context.Invoices
            .Where(i => i.CreatedAt >= register.OpeningTime && i.CreatedAt <= closingTime)
            .ToListAsync();

        decimal calculatedSystemCash = register.OpeningBalance
            + invoices
                .Where(i => i.PaymentMethod.Equals("Efectivo", StringComparison.OrdinalIgnoreCase))
                .Sum(i => i.TotalAmount);

        decimal calculatedSystemTransfer = invoices
            .Where(i => i.PaymentMethod.Equals("Transferencia", StringComparison.OrdinalIgnoreCase))
            .Sum(i => i.TotalAmount);

        decimal calculatedSystemCard = invoices
            .Where(i => i.PaymentMethod.Equals("Tarjeta", StringComparison.OrdinalIgnoreCase))
            .Sum(i => i.TotalAmount);

        register.ClosedById      = request.ClosedById;
        register.ClosingTime     = closingTime;
        register.ReportedCash    = request.ReportedCash;
        register.ReportedTransfer = request.ReportedTransfer;
        register.ReportedCard    = request.ReportedCard;
        register.SystemCash      = calculatedSystemCash;
        register.SystemTransfer  = calculatedSystemTransfer;
        register.SystemCard      = calculatedSystemCard;
        register.Observations    = request.Observations;
        register.UpdatedAt       = DateTimeOffset.UtcNow;

        // Descuento de gastos del turno para determinar el efectivo esperado real
        // Se filtra por PaymentDate con fecha local Colombia (UTC-5)
        var colombiaOffset   = TimeSpan.FromHours(-5);
        var openDateLocal    = DateOnly.FromDateTime(register.OpeningTime.ToOffset(colombiaOffset).DateTime);
        var closeDateLocal   = DateOnly.FromDateTime(closingTime.ToOffset(colombiaOffset).DateTime);

        var expensesCash = await _context.Expenses
            .Where(e => e.PaymentDate >= openDateLocal && e.PaymentDate <= closeDateLocal
                     && e.PaymentMethod == "Efectivo")
            .SumAsync(e => e.AmountPaid);

        decimal netCashExpected = calculatedSystemCash - expensesCash;

        // Caja descuadrada si diferencia de efectivo (descontando gastos) supera $1000
        decimal diff = Math.Abs(request.ReportedCash - netCashExpected);
        register.Status = diff > 1000m ? "Descuadrada" : "Cerrada";

        _context.CashRegisters.Update(register);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(register.Id);
    }

    private static CashRegisterResponse MapToResponse(CashRegister c) => new()
    {
        Id = c.Id,
        PeriodId = c.PeriodId,
        PeriodName = c.Period?.Name ?? "Desconocido",
        OpenedById = c.OpenedById,
        OpenedByName = c.OpenedBy?.Name ?? "Desconocido",
        ClosedById = c.ClosedById,
        ClosedByName = c.ClosedBy?.Name,
        OpeningTime = c.OpeningTime,
        ClosingTime = c.ClosingTime,
        OpeningBalance = c.OpeningBalance,
        ReportedCash = c.ReportedCash,
        ReportedTransfer = c.ReportedTransfer,
        ReportedCard = c.ReportedCard,
        SystemCash = c.SystemCash,
        SystemTransfer = c.SystemTransfer,
        SystemCard = c.SystemCard,
        Status = c.Status,
        Observations = c.Observations
    };
}

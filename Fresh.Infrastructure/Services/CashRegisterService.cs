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

        var orders = await _context.Orders
            .Where(o => o.CreatedAt >= register.OpeningTime && o.CreatedAt <= until
                     && o.Status != "Cancelado")
            .ToListAsync();

        decimal cashSales     = orders.Where(o => o.PaymentMethod.Equals("Efectivo",      StringComparison.OrdinalIgnoreCase)).Sum(o => o.Total);
        decimal transferSales = orders.Where(o => o.PaymentMethod.Equals("Transferencia", StringComparison.OrdinalIgnoreCase)).Sum(o => o.Total);
        decimal cardSales     = orders.Where(o => o.PaymentMethod.Equals("Tarjeta",       StringComparison.OrdinalIgnoreCase)).Sum(o => o.Total);
        decimal creditSales   = orders.Where(o => o.PaymentMethod.Equals("Crédito",       StringComparison.OrdinalIgnoreCase)
                                               || o.PaymentMethod.Equals("Credito",       StringComparison.OrdinalIgnoreCase)).Sum(o => o.Total);

        decimal systemCash     = register.OpeningBalance + cashSales;
        decimal systemTransfer = transferSales;
        decimal systemCard     = cardSales;
        decimal systemCredit   = creditSales;

        // Gastos del turno: fecha local Colombia (UTC-5)
        var colombiaOffset = TimeSpan.FromHours(-5);
        var openDateLocal  = DateOnly.FromDateTime(register.OpeningTime.ToOffset(colombiaOffset).DateTime);
        var closeDateLocal = DateOnly.FromDateTime(until.ToOffset(colombiaOffset).DateTime).AddDays(1);

        var allExpenses = await _context.Expenses
            .Include(e => e.ExpenseType)
            .ToListAsync();

        var expenses = allExpenses
            .Where(e => e.PaymentDate >= openDateLocal && e.PaymentDate <= closeDateLocal)
            .ToList();

        decimal expCash     = expenses.Where(e => e.PaymentMethod.Equals("Efectivo",      StringComparison.OrdinalIgnoreCase)).Sum(e => e.AmountPaid);
        decimal expTransfer = expenses.Where(e => e.PaymentMethod.Equals("Transferencia", StringComparison.OrdinalIgnoreCase)).Sum(e => e.AmountPaid);
        decimal expCard     = expenses.Where(e => e.PaymentMethod.Equals("Tarjeta",       StringComparison.OrdinalIgnoreCase)).Sum(e => e.AmountPaid);
        decimal totalExp    = expCash + expTransfer + expCard;

        // Abonos de crédito cobrados durante el turno
        var creditPayments = await _context.CreditTransactions
            .Where(t => t.Type == "Abono" && t.CreatedAt >= register.OpeningTime && t.CreatedAt <= until)
            .ToListAsync();

        decimal cpCash     = creditPayments.Where(t => (t.PaymentMethod ?? "Efectivo").Equals("Efectivo",      StringComparison.OrdinalIgnoreCase)).Sum(t => t.Amount);
        decimal cpTransfer = creditPayments.Where(t => (t.PaymentMethod ?? "").Equals("Transferencia", StringComparison.OrdinalIgnoreCase)).Sum(t => t.Amount);
        decimal cpTotal    = cpCash + cpTransfer;

        // Neto: ventas + abonos crédito − gastos
        decimal netCash     = systemCash     + cpCash     - expCash;
        decimal netTransfer = systemTransfer + cpTransfer - expTransfer;
        decimal netCard     = systemCard                  - expCard;

        return new CashSystemTotalsResponse
        {
            RegisterId           = id,
            OpeningBalance       = register.OpeningBalance,
            SystemCash           = systemCash,
            SystemTransfer       = systemTransfer,
            SystemCard           = systemCard,
            SystemCredit         = systemCredit,
            SalesCash            = cashSales,
            SalesTransfer        = transferSales,
            TotalInvoices        = cashSales + transferSales + cardSales + creditSales,
            InvoiceCount         = orders.Count,
            CreditPaymentsCash     = cpCash,
            CreditPaymentsTransfer = cpTransfer,
            CreditPaymentsTotal    = cpTotal,
            ExpensesCash         = expCash,
            ExpensesTransfer     = expTransfer,
            ExpensesCard         = expCard,
            TotalExpenses        = totalExp,
            ExpenseCount         = expenses.Count,
            Expenses             = expenses.Select(e => new ExpenseItemDto
            {
                Id              = e.Id,
                ExpenseTypeName = e.ExpenseType?.Name ?? "",
                AmountPaid      = e.AmountPaid,
                PaymentMethod   = e.PaymentMethod,
                PaymentDate     = e.PaymentDate,
                CreatedAt       = e.CreatedAt,
                Notes           = e.Notes
            }).ToList(),
            NetCash     = netCash,
            NetTransfer = netTransfer,
            NetCard     = netCard,
        };
    }

    public async Task<CashRegisterResponse?> CloseRegisterAsync(int id, CloseCashRegisterRequest request)
    {
        var register = await _context.CashRegisters.FindAsync(id);
        if (register == null) return null;
        if (register.Status != "Abierta")
            throw new InvalidOperationException("Esta caja ya fue cerrada.");

        var closingTime = DateTimeOffset.UtcNow;

        var orders = await _context.Orders
            .Where(o => o.CreatedAt >= register.OpeningTime && o.CreatedAt <= closingTime
                     && o.Status != "Cancelado")
            .ToListAsync();

        decimal salesCash     = orders.Where(o => o.PaymentMethod.Equals("Efectivo",      StringComparison.OrdinalIgnoreCase)).Sum(o => o.Total);
        decimal salesTransfer = orders.Where(o => o.PaymentMethod.Equals("Transferencia", StringComparison.OrdinalIgnoreCase)).Sum(o => o.Total);
        decimal salesCard     = orders.Where(o => o.PaymentMethod.Equals("Tarjeta",       StringComparison.OrdinalIgnoreCase)).Sum(o => o.Total);

        decimal calculatedSystemCash     = register.OpeningBalance + salesCash;
        decimal calculatedSystemTransfer = salesTransfer;
        decimal calculatedSystemCard     = salesCard;
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

        // Gastos confirmados por el usuario
        var colombiaOffset = TimeSpan.FromHours(-5);
        var openDateLocal  = DateOnly.FromDateTime(register.OpeningTime.ToOffset(colombiaOffset).DateTime);
        var closeDateLocal = DateOnly.FromDateTime(closingTime.ToOffset(colombiaOffset).DateTime).AddDays(1);

        var allExpenses = await _context.Expenses
            .Where(e => e.PaymentDate >= openDateLocal && e.PaymentDate <= closeDateLocal)
            .ToListAsync();

        // Si el usuario envió lista de IDs seleccionados, usar solo esos; si no, usar todos
        var activeExpenses = request.SelectedExpenseIds is { Count: > 0 }
            ? allExpenses.Where(e => request.SelectedExpenseIds.Contains(e.Id)).ToList()
            : allExpenses;

        decimal expCash     = activeExpenses.Where(e => e.PaymentMethod.Equals("Efectivo",      StringComparison.OrdinalIgnoreCase)).Sum(e => e.AmountPaid);
        decimal expTransfer = activeExpenses.Where(e => e.PaymentMethod.Equals("Transferencia", StringComparison.OrdinalIgnoreCase)).Sum(e => e.AmountPaid);

        // Abonos de crédito cobrados durante el turno
        var creditPayments = await _context.CreditTransactions
            .Where(t => t.Type == "Abono" && t.CreatedAt >= register.OpeningTime && t.CreatedAt <= closingTime)
            .ToListAsync();

        decimal cpCash     = creditPayments.Where(t => (t.PaymentMethod ?? "Efectivo").Equals("Efectivo",      StringComparison.OrdinalIgnoreCase)).Sum(t => t.Amount);
        decimal cpTransfer = creditPayments.Where(t => (t.PaymentMethod ?? "").Equals("Transferencia", StringComparison.OrdinalIgnoreCase)).Sum(t => t.Amount);

        decimal netCashExpected     = calculatedSystemCash     + cpCash     - expCash;
        decimal netTransferExpected = calculatedSystemTransfer + cpTransfer - expTransfer;
        decimal totalMovableExpected = netCashExpected + netTransferExpected;
        decimal totalMovableReported = request.ReportedCash + request.ReportedTransfer;

        decimal signedDiff = totalMovableReported - totalMovableExpected;
        register.Difference          = signedDiff;
        register.Status              = Math.Abs(signedDiff) > 1000m ? "Descuadrada" : "Cerrada";
        register.AmountToSafe        = request.AmountToSafe;
        register.AmountToBankAccount = request.AmountToBankAccount;
        register.AmountLeftInRegister = request.AmountLeftInRegister;

        _context.CashRegisters.Update(register);

        // Depositar a caja fuerte
        if (request.AmountToSafe > 0)
        {
            var safe = await _context.Safes.FirstOrDefaultAsync(s => s.SafeType == "caja_fuerte");
            if (safe == null)
            {
                safe = new Core.Entities.Safe { SafeType = "caja_fuerte" };
                _context.Safes.Add(safe);
                await _context.SaveChangesAsync();
            }
            var before     = safe.Balance;
            safe.Balance  += request.AmountToSafe;
            safe.UpdatedAt = DateTimeOffset.UtcNow;
            _context.SafeTransactions.Add(new Core.Entities.SafeTransaction
            {
                Type           = "Ingreso",
                Amount         = request.AmountToSafe,
                Description    = $"Cierre de caja #{id}",
                BalanceBefore  = before,
                BalanceAfter   = safe.Balance,
                CashRegisterId = id,
                CreatedById    = request.ClosedById,
                SafeType       = "caja_fuerte",
            });
        }

        // Depositar a cuenta bancaria
        if (request.AmountToBankAccount > 0)
        {
            var bank = await _context.Safes.FirstOrDefaultAsync(s => s.SafeType == "cuenta_bancaria");
            if (bank == null)
            {
                bank = new Core.Entities.Safe { SafeType = "cuenta_bancaria" };
                _context.Safes.Add(bank);
                await _context.SaveChangesAsync();
            }
            var before     = bank.Balance;
            bank.Balance  += request.AmountToBankAccount;
            bank.UpdatedAt = DateTimeOffset.UtcNow;
            _context.SafeTransactions.Add(new Core.Entities.SafeTransaction
            {
                Type           = "Ingreso",
                Amount         = request.AmountToBankAccount,
                Description    = $"Cierre de caja #{id}",
                BalanceBefore  = before,
                BalanceAfter   = bank.Balance,
                CashRegisterId = id,
                CreatedById    = request.ClosedById,
                SafeType       = "cuenta_bancaria",
            });
        }

        await _context.SaveChangesAsync();

        return await GetByIdAsync(register.Id);
    }

    public async Task<CashRegisterResponse?> EditAsync(int id, EditCashRegisterRequest request)
    {
        var register = await _context.CashRegisters.FindAsync(id);
        if (register == null) return null;
        if (register.Status == "Abierta")
            throw new InvalidOperationException("No se puede editar una caja abierta. Ciérrala primero.");

        register.ReportedCash     = request.ReportedCash;
        register.ReportedTransfer = request.ReportedTransfer;
        register.ReportedCard     = request.ReportedCard;
        register.Observations     = request.Observations;
        register.UpdatedAt        = DateTimeOffset.UtcNow;

        // Recalcular estado con la misma lógica del cierre:
        // Tarjeta excluida; se compara el total movible (efectivo + transferencia).
        decimal netCashExpected     = (register.SystemCash     ?? 0);
        decimal netTransferExpected = (register.SystemTransfer ?? 0);

        // Descontar gastos ya registrados en el turno
        if (register.ClosingTime.HasValue)
        {
            var colombiaOffset = TimeSpan.FromHours(-5);
            var openDateLocal  = DateOnly.FromDateTime(register.OpeningTime.ToOffset(colombiaOffset).DateTime);
            var closeDateLocal = DateOnly.FromDateTime(register.ClosingTime.Value.ToOffset(colombiaOffset).DateTime).AddDays(1);

            var allExpenses = (await _context.Expenses.ToListAsync())
                .Where(e => e.PaymentDate >= openDateLocal && e.PaymentDate <= closeDateLocal)
                .ToList();

            netCashExpected     -= allExpenses.Where(e => e.PaymentMethod.Equals("Efectivo",      StringComparison.OrdinalIgnoreCase)).Sum(e => e.AmountPaid);
            netTransferExpected -= allExpenses.Where(e => e.PaymentMethod.Equals("Transferencia", StringComparison.OrdinalIgnoreCase)).Sum(e => e.AmountPaid);
        }

        decimal totalMovableExpected = netCashExpected + netTransferExpected;
        decimal totalMovableReported = request.ReportedCash + request.ReportedTransfer;
        decimal signedDiff = totalMovableReported - totalMovableExpected;
        register.Difference = signedDiff;
        register.Status = Math.Abs(signedDiff) > 1000m ? "Descuadrada" : "Cerrada";

        _context.CashRegisters.Update(register);
        await _context.SaveChangesAsync();
        return await GetByIdAsync(register.Id);
    }

    public async Task<CashRegisterResponse?> UpdateOpeningBalanceAsync(int id, decimal openingBalance)
    {
        var register = await _context.CashRegisters.FindAsync(id);
        if (register == null) return null;
        if (register.Status != "Abierta")
            throw new InvalidOperationException("Solo se puede editar el saldo inicial de una caja abierta.");
        if (openingBalance < 0)
            throw new InvalidOperationException("El saldo inicial no puede ser negativo.");

        register.OpeningBalance = openingBalance;
        register.UpdatedAt = DateTimeOffset.UtcNow;
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
        CashDifference = c.Difference,
        Status = c.Status,
        Observations = c.Observations,
        AmountToSafe = c.AmountToSafe,
        AmountToBankAccount = c.AmountToBankAccount,
        AmountLeftInRegister = c.AmountLeftInRegister,
    };
}

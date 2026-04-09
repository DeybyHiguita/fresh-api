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

        // Usamos PEDIDOS (igual que el dashboard) para que ambas vistas sean consistentes.
        // Las facturas son opcionales y no se crean para todos los pedidos, lo que causaba
        // que la caja mostrara menos ventas que el dashboard. Los pedidos cancelados se excluyen.
        var orders = await _context.Orders
            .Where(o => o.CreatedAt >= register.OpeningTime && o.CreatedAt <= until
                     && o.Status != "Cancelado")
            .ToListAsync();

        decimal cashSales     = orders.Where(o => o.PaymentMethod.Equals("Efectivo",      StringComparison.OrdinalIgnoreCase)).Sum(o => o.Total);
        decimal transferSales = orders.Where(o => o.PaymentMethod.Equals("Transferencia", StringComparison.OrdinalIgnoreCase)).Sum(o => o.Total);
        decimal cardSales     = orders.Where(o => o.PaymentMethod.Equals("Tarjeta",       StringComparison.OrdinalIgnoreCase)).Sum(o => o.Total);
        decimal creditSales   = orders.Where(o => o.PaymentMethod.Equals("Crédito",       StringComparison.OrdinalIgnoreCase)
                                               || o.PaymentMethod.Equals("Credito",       StringComparison.OrdinalIgnoreCase)).Sum(o => o.Total);

        // Efectivo en cajón = saldo de apertura + ventas en efectivo
        decimal systemCash     = register.OpeningBalance + cashSales;
        decimal systemTransfer = transferSales;
        decimal systemCard     = cardSales;
        decimal systemCredit   = creditSales;

        // Gastos del turno: se filtra por PaymentDate (fecha local Colombia UTC-5)
        var colombiaOffset = TimeSpan.FromHours(-5);
        var openDateLocal  = DateOnly.FromDateTime(register.OpeningTime.ToOffset(colombiaOffset).DateTime);
        var closeDateLocal = DateOnly.FromDateTime(until.ToOffset(colombiaOffset).DateTime).AddDays(1);

        var expenses = (await _context.Expenses.ToListAsync())
            .Where(e => e.PaymentDate >= openDateLocal && e.PaymentDate <= closeDateLocal)
            .ToList();

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
            SystemCredit     = systemCredit,
            TotalInvoices    = cashSales + transferSales + cardSales + creditSales,
            InvoiceCount     = orders.Count,
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

        // Calcular totales del sistema desde PEDIDOS (igual que el dashboard).
        // Las facturas son opcionales: no todos los pedidos tienen factura, lo que genera
        // discrepancias. Los pedidos cancelados se excluyen.
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

        // Descuento de gastos del turno para determinar el efectivo esperado real
        // Se filtra por PaymentDate con fecha local Colombia (UTC-5)
        // closeDateLocal +1: captura gastos registrados después de las 7PM Colombia.
        var colombiaOffset   = TimeSpan.FromHours(-5);
        var openDateLocal    = DateOnly.FromDateTime(register.OpeningTime.ToOffset(colombiaOffset).DateTime);
        var closeDateLocal   = DateOnly.FromDateTime(closingTime.ToOffset(colombiaOffset).DateTime).AddDays(1);

        var allExpenses = (await _context.Expenses.ToListAsync())
            .Where(e => e.PaymentDate >= openDateLocal && e.PaymentDate <= closeDateLocal)
            .ToList();

        decimal expCash     = allExpenses.Where(e => e.PaymentMethod.Equals("Efectivo",      StringComparison.OrdinalIgnoreCase)).Sum(e => e.AmountPaid);
        decimal expTransfer = allExpenses.Where(e => e.PaymentMethod.Equals("Transferencia", StringComparison.OrdinalIgnoreCase)).Sum(e => e.AmountPaid);

        // Tarjeta NO cuenta como dinero físico en el cajón (va al datafono).
        // La caja se evalúa comparando el TOTAL movible (efectivo + transferencia).
        // Si la suma cuadra, da igual cómo se distribuya entre los dos métodos.
        decimal netCashExpected     = calculatedSystemCash     - expCash;
        decimal netTransferExpected = calculatedSystemTransfer - expTransfer;
        decimal totalMovableExpected = netCashExpected + netTransferExpected;
        decimal totalMovableReported = request.ReportedCash + request.ReportedTransfer;

        decimal diff = Math.Abs(totalMovableReported - totalMovableExpected);
        register.Status              = diff > 1000m ? "Descuadrada" : "Cerrada";
        register.AmountToSafe        = request.AmountToSafe;
        register.AmountToBankAccount = request.AmountToBankAccount;

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
        decimal diff = Math.Abs(totalMovableReported - totalMovableExpected);
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
        Observations = c.Observations,
        AmountToSafe = c.AmountToSafe,
        AmountToBankAccount = c.AmountToBankAccount,
    };
}

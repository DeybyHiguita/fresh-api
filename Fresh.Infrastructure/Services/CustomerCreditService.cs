using Fresh.Core.DTOs.CustomerCredit;
using Fresh.Core.Entities;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fresh.Infrastructure.Services;

public class CustomerCreditService : ICustomerCreditService
{
    private readonly FreshDbContext _context;

    public CustomerCreditService(FreshDbContext context) { _context = context; }

    public async Task<CustomerCreditResponse?> GetByCustomerIdAsync(int customerId)
    {
        var credit = await _context.CustomerCredits
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId);
        return credit == null ? null : MapToResponse(credit);
    }

    public async Task<CustomerCreditResponse> CreateOrUpdateConfigAsync(CustomerCreditRequest request)
    {
        var customerExists = await _context.Customers.AnyAsync(c => c.Id == request.CustomerId);
        if (!customerExists) throw new KeyNotFoundException("El cliente no existe.");

        var credit = await _context.CustomerCredits.FirstOrDefaultAsync(c => c.CustomerId == request.CustomerId);

        if (credit == null)
        {
            credit = new CustomerCredit
            {
                CustomerId = request.CustomerId,
                CreditLimit = request.CreditLimit,
                PaymentFrequency = request.PaymentFrequency,
                CurrentBalance = 0m,
                Status = "Al día",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            _context.CustomerCredits.Add(credit);
        }
        else
        {
            credit.CreditLimit = request.CreditLimit;
            credit.PaymentFrequency = request.PaymentFrequency;
            credit.UpdatedAt = DateTimeOffset.UtcNow;
            // Recalcular status por si el límite cambió
            credit.Status = credit.CurrentBalance <= 0 ? "Al día" : "Con deuda";
            _context.CustomerCredits.Update(credit);
        }

        await _context.SaveChangesAsync();
        return await GetByCustomerIdAsync(request.CustomerId) ?? throw new Exception("Error mapeando crédito.");
    }

    public async Task<CustomerCreditResponse?> RegisterPaymentAsync(int id, CreditPaymentRequest request)
    {
        var credit = await _context.CustomerCredits.FindAsync(id);
        if (credit == null) return null;

        if (request.Amount > credit.CurrentBalance)
            throw new InvalidOperationException($"El cliente solo debe ${credit.CurrentBalance}.");

        decimal balanceBefore = credit.CurrentBalance;
        credit.CurrentBalance -= request.Amount;
        credit.Status = credit.CurrentBalance <= 0 ? "Al día" : "Con deuda";
        credit.UpdatedAt = DateTimeOffset.UtcNow;
        _context.CustomerCredits.Update(credit);

        var description = "Pago registrado manualmente";
        if (!string.IsNullOrWhiteSpace(request.PaymentMethod))
            description += $" ({request.PaymentMethod})";
        if (!string.IsNullOrWhiteSpace(request.Notes))
            description += $": {request.Notes}";

        _context.CreditTransactions.Add(new CreditTransaction
        {
            CustomerCreditId = credit.Id,
            OrderId = null,
            Type = "Abono",
            Amount = request.Amount,
            BalanceBefore = balanceBefore,
            BalanceAfter = credit.CurrentBalance,
            Description = description,
            PaymentMethod = request.PaymentMethod,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await _context.SaveChangesAsync();
        return await GetByCustomerIdAsync(credit.CustomerId);
    }

    public async Task<IEnumerable<CreditTransactionResponse>> GetTransactionsAsync(int customerId)
    {
        var credit = await _context.CustomerCredits.FirstOrDefaultAsync(c => c.CustomerId == customerId);
        if (credit == null) return [];

        return await _context.CreditTransactions
            .Where(t => t.CustomerCreditId == credit.Id)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new CreditTransactionResponse
            {
                Id = t.Id,
                CustomerCreditId = t.CustomerCreditId,
                OrderId = t.OrderId,
                Type = t.Type,
                Amount = t.Amount,
                BalanceBefore = t.BalanceBefore,
                BalanceAfter = t.BalanceAfter,
                Description = t.Description,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<CreditOrderResponse>> GetCreditOrdersAsync(int customerId)
    {
        var orders = await _context.Orders
            .Where(o => o.CustomerId == customerId && o.PaymentMethod == "Crédito")
            .Include(o => o.OrderItems).ThenInclude(i => i.MenuItem)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(o => new CreditOrderResponse
        {
            OrderId  = o.Id,
            CustomerName = o.CustomerName,
            Subtotal = o.Subtotal,
            Discount = o.Discount,
            Total    = o.Total,
            Status   = o.Status,
            IsCreditPaid = o.IsCreditPaid,
            CreatedAt = o.CreatedAt,
            Notes    = o.Notes,
            Items    = o.OrderItems.Select(i => new CreditOrderItemResponse
            {
                MenuItemId   = i.MenuItemId,
                MenuItemName = i.MenuItem?.Name ?? "Producto",
                Quantity     = i.Quantity,
                UnitPrice    = i.UnitPrice,
                Subtotal     = i.UnitPrice * i.Quantity,
                ItemNotes    = i.ItemNotes,
            }).ToList(),
        }).ToList();
    }

    public async Task<CustomerCreditResponse> PayOrdersAsync(int creditId, PayOrdersRequest request)
    {
        var credit = await _context.CustomerCredits.FindAsync(creditId)
            ?? throw new KeyNotFoundException("Cuenta de crédito no encontrada.");

        if (request.OrderIds.Count == 0)
            throw new InvalidOperationException("Selecciona al menos una orden para pagar.");

        var orders = await _context.Orders
            .Where(o => request.OrderIds.Contains(o.Id)
                     && o.CustomerId == credit.CustomerId
                     && o.PaymentMethod == "Crédito"
                     && !o.IsCreditPaid)
            .ToListAsync();

        if (orders.Count == 0)
            throw new InvalidOperationException("No se encontraron órdenes válidas para pagar.");

        decimal totalToPay = orders.Sum(o => o.Total);

        if (totalToPay > credit.CurrentBalance)
            throw new InvalidOperationException($"El monto a pagar (${totalToPay}) supera el saldo adeudado (${credit.CurrentBalance}).");

        decimal balanceBefore = credit.CurrentBalance;
        credit.CurrentBalance -= totalToPay;
        credit.Status = credit.CurrentBalance <= 0 ? "Al día" : "Con deuda";
        credit.UpdatedAt = DateTimeOffset.UtcNow;

        foreach (var order in orders)
            order.IsCreditPaid = true;

        var orderIds = string.Join(", #", orders.Select(o => o.Id));
        var description = $"Pago de órdenes #{orderIds}";
        if (!string.IsNullOrWhiteSpace(request.PaymentMethod)) description += $" ({request.PaymentMethod})";
        if (!string.IsNullOrWhiteSpace(request.Notes)) description += $": {request.Notes}";

        _context.CreditTransactions.Add(new CreditTransaction
        {
            CustomerCreditId = credit.Id,
            OrderId = null,
            Type = "Abono",
            Amount = totalToPay,
            BalanceBefore = balanceBefore,
            BalanceAfter = credit.CurrentBalance,
            Description = description,
            PaymentMethod = request.PaymentMethod,
            CreatedAt = DateTimeOffset.UtcNow,
        });

        await _context.SaveChangesAsync();
        return await GetByCustomerIdAsync(credit.CustomerId) ?? throw new Exception("Error mapeando crédito.");
    }

    public async Task<CustomerCreditResponse?> RegisterPurchaseAsync(int customerId, decimal purchaseAmount)
    {
        var credit = await _context.CustomerCredits.FirstOrDefaultAsync(c => c.CustomerId == customerId);
        if (credit == null) throw new InvalidOperationException("El cliente no tiene cuenta de crédito autorizada.");

        if (credit.Status == "Bloqueado") throw new InvalidOperationException("El crédito de este cliente está bloqueado.");

        if ((credit.CurrentBalance + purchaseAmount) > credit.CreditLimit)
            throw new InvalidOperationException($"La compra excede el límite de crédito disponible (${credit.CreditLimit - credit.CurrentBalance}).");

        credit.CurrentBalance += purchaseAmount;
        credit.Status = credit.CurrentBalance <= 0 ? "Al día" : "Con deuda";
        credit.UpdatedAt = DateTimeOffset.UtcNow;
        _context.CustomerCredits.Update(credit);
        await _context.SaveChangesAsync();

        return await GetByCustomerIdAsync(customerId);
    }

    public async Task<IEnumerable<PaidDebtReportResponse>> GetPaidPaymentsAsync(DateTimeOffset from, DateTimeOffset to)
    {
        var fromUtc = from.ToUniversalTime();
        var toUtc   = to.ToUniversalTime();

        var rows = await _context.CreditTransactions
            .Where(t => t.Type == "Abono" && t.CreatedAt >= fromUtc && t.CreatedAt <= toUtc)
            .Include(t => t.CustomerCredit)
                .ThenInclude(cc => cc.Customer)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return rows.Select(t => new PaidDebtReportResponse
        {
            TransactionId    = t.Id,
            CustomerId       = t.CustomerCredit?.CustomerId ?? 0,
            CustomerName     = t.CustomerCredit?.Customer != null
                               ? $"{t.CustomerCredit.Customer.FirstName} {t.CustomerCredit.Customer.LastName}"
                               : "Desconocido",
            CustomerDocument = t.CustomerCredit?.Customer?.DocumentNumber,
            CustomerPhone    = t.CustomerCredit?.Customer?.Phone,
            Amount           = t.Amount,
            BalanceBefore    = t.BalanceBefore,
            BalanceAfter     = t.BalanceAfter,
            PaymentMethod    = t.PaymentMethod,
            Description      = t.Description,
            CreatedAt        = t.CreatedAt,
        }).ToList();
    }

    private static CustomerCreditResponse MapToResponse(CustomerCredit c) => new()
    {
        Id = c.Id,
        CustomerId = c.CustomerId,
        CustomerName = c.Customer != null ? $"{c.Customer.FirstName} {c.Customer.LastName}" : "Desconocido",
        CreditLimit = c.CreditLimit,
        PaymentFrequency = c.PaymentFrequency,
        CurrentBalance = c.CurrentBalance,
        Status = c.Status,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt
    };
}
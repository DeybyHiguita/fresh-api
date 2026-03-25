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
        credit.Status = credit.CurrentBalance <= 0 ? "Al día"
                       : credit.CurrentBalance < credit.CreditLimit ? "Con deuda"
                       : "Límite alcanzado";
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

    public async Task<CustomerCreditResponse?> RegisterPurchaseAsync(int customerId, decimal purchaseAmount)
    {
        var credit = await _context.CustomerCredits.FirstOrDefaultAsync(c => c.CustomerId == customerId);
        if (credit == null) throw new InvalidOperationException("El cliente no tiene cuenta de crédito autorizada.");

        if (credit.Status == "Bloqueado") throw new InvalidOperationException("El crédito de este cliente está bloqueado.");

        if ((credit.CurrentBalance + purchaseAmount) > credit.CreditLimit)
            throw new InvalidOperationException($"La compra excede el límite de crédito disponible (${credit.CreditLimit - credit.CurrentBalance}).");

        credit.CurrentBalance += purchaseAmount;
        if (credit.CurrentBalance > 0 && credit.Status == "Al día") credit.Status = "Con deuda";

        credit.UpdatedAt = DateTimeOffset.UtcNow;
        _context.CustomerCredits.Update(credit);
        await _context.SaveChangesAsync();

        return await GetByCustomerIdAsync(customerId);
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
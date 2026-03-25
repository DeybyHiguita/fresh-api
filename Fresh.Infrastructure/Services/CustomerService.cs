using Fresh.Core.DTOs.Customer;
using Fresh.Core.Entities;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fresh.Infrastructure.Services;

public class CustomerService : ICustomerService
{
    private readonly FreshDbContext _context;

    public CustomerService(FreshDbContext context) { _context = context; }

    public async Task<IEnumerable<CustomerResponse>> GetAllAsync(bool onlyActive = true)
    {
        var query = _context.Customers
            .Include(c => c.CreatedBy)
            .Include(c => c.CreditInfo)
            .AsQueryable();

        if (onlyActive) query = query.Where(c => c.IsActive);
        var customers = await query.OrderBy(c => c.FirstName).ToListAsync();
        return customers.Select(MapToResponse);
    }

    public async Task<CustomerResponse?> GetByIdAsync(int id)
    {
        var customer = await _context.Customers
            .Include(c => c.CreatedBy)
            .Include(c => c.CreditInfo)
            .FirstOrDefaultAsync(c => c.Id == id);
        return customer == null ? null : MapToResponse(customer);
    }

    public async Task<CustomerResponse> CreateAsync(CustomerRequest request)
    {
        var exists = await _context.Customers.AnyAsync(c => c.DocumentNumber == request.DocumentNumber && c.IsActive);
        if (exists) throw new InvalidOperationException($"Ya existe un cliente activo con el documento '{request.DocumentNumber}'");
        // If a soft-deleted customer with the same document exists, reactivate them
        var inactive = await _context.Customers.FirstOrDefaultAsync(c => c.DocumentNumber == request.DocumentNumber && !c.IsActive);
        if (inactive != null)
        {
            inactive.FirstName = request.FirstName;
            inactive.LastName = request.LastName;
            inactive.Phone = request.Phone;
            inactive.Address = request.Address;
            inactive.ReferenceName = request.ReferenceName;
            inactive.ReferencePhone = request.ReferencePhone;
            inactive.IsActive = true;
            inactive.UpdatedAt = DateTimeOffset.UtcNow;
            _context.Customers.Update(inactive);
            await _context.SaveChangesAsync();
            return await GetByIdAsync(inactive.Id) ?? throw new Exception("Error mapeando cliente.");
        }
        var userExists = await _context.Users.AnyAsync(u => u.Id == request.CreatedById);
        if (!userExists) throw new KeyNotFoundException("El cajero responsable no existe.");

        var customer = new Customer
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            DocumentNumber = request.DocumentNumber,
            Phone = request.Phone,
            Address = request.Address,
            ReferenceName = request.ReferenceName,
            ReferencePhone = request.ReferencePhone,
            CreatedById = request.CreatedById,
            IsActive = request.IsActive,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        return await GetByIdAsync(customer.Id) ?? throw new Exception("Error mapeando cliente.");
    }

    public async Task<CustomerResponse?> UpdateAsync(int id, CustomerRequest request)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null) return null;

        var exists = await _context.Customers.AnyAsync(c => c.DocumentNumber == request.DocumentNumber && c.Id != id);
        if (exists) throw new InvalidOperationException($"El documento '{request.DocumentNumber}' ya está en uso.");

        customer.FirstName = request.FirstName;
        customer.LastName = request.LastName;
        customer.DocumentNumber = request.DocumentNumber;
        customer.Phone = request.Phone;
        customer.Address = request.Address;
        customer.ReferenceName = request.ReferenceName;
        customer.ReferencePhone = request.ReferencePhone;
        customer.IsActive = request.IsActive;
        customer.UpdatedAt = DateTimeOffset.UtcNow;

        _context.Customers.Update(customer);
        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null) return false;

        customer.IsActive = false;
        customer.UpdatedAt = DateTimeOffset.UtcNow;
        _context.Customers.Update(customer);
        await _context.SaveChangesAsync();
        return true;
    }

    private static CustomerResponse MapToResponse(Customer c) => new()
    {
        Id = c.Id,
        FirstName = c.FirstName,
        LastName = c.LastName,
        DocumentNumber = c.DocumentNumber,
        Phone = c.Phone,
        Address = c.Address,
        ReferenceName = c.ReferenceName,
        ReferencePhone = c.ReferencePhone,
        CreatedById = c.CreatedById,
        CreatedByName = c.CreatedBy?.Name ?? "Desconocido",
        IsActive = c.IsActive,
        CreatedAt = c.CreatedAt,

        HasCreditAccount = c.CreditInfo != null,
        CreditLimit = c.CreditInfo?.CreditLimit,
        CurrentBalance = c.CreditInfo?.CurrentBalance,
        CreditStatus = c.CreditInfo?.Status
    };
}
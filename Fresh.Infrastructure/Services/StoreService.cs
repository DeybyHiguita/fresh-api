using Fresh.Core.DTOs.Store;
using Fresh.Core.Entities;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fresh.Infrastructure.Services;

public class StoreService : IStoreService
{
    private readonly FreshDbContext _context;

    public StoreService(FreshDbContext context) { _context = context; }

    public async Task<IEnumerable<StoreResponse>> GetAllAsync()
    {
        var stores = await _context.Stores.OrderBy(s => s.Name).ToListAsync();
        return stores.Select(MapToResponse);
    }

    public async Task<StoreResponse?> GetByIdAsync(int id)
    {
        var store = await _context.Stores.FindAsync(id);
        return store == null ? null : MapToResponse(store);
    }

    public async Task<StoreResponse> CreateAsync(StoreRequest request)
    {
        var store = new Store
        {
            Name      = request.Name,
            Address   = request.Address,
            Phone     = request.Phone,
            IsActive  = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Stores.Add(store);
        await _context.SaveChangesAsync();
        return MapToResponse(store);
    }

    public async Task<StoreResponse?> UpdateAsync(int id, StoreRequest request)
    {
        var store = await _context.Stores.FindAsync(id);
        if (store == null) return null;

        store.Name      = request.Name;
        store.Address   = request.Address;
        store.Phone     = request.Phone;
        store.IsActive  = request.IsActive;
        store.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToResponse(store);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var store = await _context.Stores.FindAsync(id);
        if (store == null) return false;

        _context.Stores.Remove(store);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task AddUserToStoreAsync(int storeId, int userId, bool isDefault = false)
    {
        var exists = await _context.UserStores.AnyAsync(us => us.StoreId == storeId && us.UserId == userId);
        if (exists) return;

        if (isDefault)
        {
            var currentDefaults = await _context.UserStores
                .Where(us => us.UserId == userId && us.IsDefault)
                .ToListAsync();
            currentDefaults.ForEach(us => us.IsDefault = false);
        }

        _context.UserStores.Add(new UserStore
        {
            StoreId   = storeId,
            UserId    = userId,
            IsDefault = isDefault,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
    }

    public async Task RemoveUserFromStoreAsync(int storeId, int userId)
    {
        var link = await _context.UserStores
            .FirstOrDefaultAsync(us => us.StoreId == storeId && us.UserId == userId);
        if (link != null)
        {
            _context.UserStores.Remove(link);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<StoreSummary>> GetUserStoresAsync(int userId)
    {
        return await _context.UserStores
            .Where(us => us.UserId == userId && us.Store.IsActive)
            .Select(us => new StoreSummary { Id = us.StoreId, Name = us.Store.Name, IsDefault = us.IsDefault })
            .ToListAsync();
    }

    public async Task<IEnumerable<StoreUserResponse>> GetStoreUsersAsync(int storeId)
    {
        return await _context.UserStores
            .Where(us => us.StoreId == storeId)
            .Select(us => new StoreUserResponse
            {
                Id        = us.UserId,
                Name      = us.User.Name,
                Email     = us.User.Email,
                Role      = us.User.Role,
                IsDefault = us.IsDefault,
            })
            .ToListAsync();
    }

    private static StoreResponse MapToResponse(Store s) => new()
    {
        Id        = s.Id,
        Name      = s.Name,
        Address   = s.Address,
        Phone     = s.Phone,
        IsActive  = s.IsActive,
        CreatedAt = s.CreatedAt
    };
}

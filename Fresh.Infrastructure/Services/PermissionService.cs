using Fresh.Core.DTOs.Permission;
using Fresh.Core.Entities;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fresh.Infrastructure.Services;

public class PermissionService : IPermissionService
{
    private readonly FreshDbContext _context;

    private static readonly string[] AllPages =
    [
        "dashboard", "recipes", "ingredients", "inventory",
        "orders", "menu-items", "cash-registers", "work-shifts",
        "customers", "expenses", "equipments"
    ];

    public PermissionService(FreshDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<UserPermissionsResponse>> GetAllUsersAsync()
    {
        var users = await _context.Users
            .OrderBy(u => u.Name)
            .ToListAsync();

        var allPerms = await _context.UserPermissions.ToListAsync();

        return users.Select(u => BuildResponse(u, allPerms.Where(p => p.UserId == u.Id)));
    }

    public async Task<UserPermissionsResponse> GetByUserIdAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException($"Usuario {userId} no encontrado.");

        var perms = await _context.UserPermissions
            .Where(p => p.UserId == userId)
            .ToListAsync();

        return BuildResponse(user, perms);
    }

    public async Task<UserPermissionsResponse> UpdateAsync(int userId, UpdateUserPermissionsRequest request)
    {
        var user = await _context.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException($"Usuario {userId} no encontrado.");

        var existing = await _context.UserPermissions
            .Where(p => p.UserId == userId)
            .ToListAsync();

        foreach (var (page, canAccess) in request.Pages)
        {
            var perm = existing.FirstOrDefault(p => p.Page == page);
            if (perm == null)
            {
                _context.UserPermissions.Add(new UserPermission
                {
                    UserId = userId,
                    Page = page,
                    CanAccess = canAccess,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                perm.CanAccess = canAccess;
                perm.UpdatedAt = DateTime.UtcNow;
                _context.UserPermissions.Update(perm);
            }
        }

        await _context.SaveChangesAsync();
        return await GetByUserIdAsync(userId);
    }

    public async Task InitializeAdminAsync(int userId)
    {
        var existing = await _context.UserPermissions
            .Where(p => p.UserId == userId)
            .Select(p => p.Page)
            .ToListAsync();

        var missing = AllPages.Except(existing);

        foreach (var page in missing)
        {
            _context.UserPermissions.Add(new UserPermission
            {
                UserId = userId,
                Page = page,
                CanAccess = true,
                UpdatedAt = DateTime.UtcNow
            });
        }

        if (missing.Any()) await _context.SaveChangesAsync();
    }

    private static UserPermissionsResponse BuildResponse(
        Fresh.Core.Entities.User user,
        IEnumerable<UserPermission> perms)
    {
        var permDict = perms.ToDictionary(p => p.Page, p => p.CanAccess);

        // Construir dict completo con todas las páginas (default false)
        var pages = AllPages.ToDictionary(p => p, p => permDict.GetValueOrDefault(p, false));

        return new UserPermissionsResponse
        {
            UserId = user.Id,
            UserName = user.Name,
            Email = user.Email,
            Role = user.Role,
            IsActive = user.IsActive,
            Pages = pages
        };
    }
}

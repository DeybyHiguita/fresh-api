using Fresh.Core.DTOs.Permission;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fresh.Infrastructure.Services;

public class PermissionService : IPermissionService
{
    private readonly FreshDbContext _context;

    public PermissionService(FreshDbContext context) { _context = context; }

    public async Task<IEnumerable<UserPermissionsResponse>> GetAllUsersAsync()
    {
        var users = await _context.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name)
            .ToListAsync();

        var allPages = await _context.AppPages
            .Where(p => p.IsActive)
            .ToListAsync();

        var allPermissions = await _context.UserPermissions
            .Include(up => up.Page)
            .ToListAsync();

        return users.Select(u => BuildResponse(u, allPages, allPermissions));
    }

    public async Task<UserPermissionsResponse> GetByUserIdAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException($"Usuario {userId} no encontrado.");

        var allPages = await _context.AppPages
            .Where(p => p.IsActive)
            .ToListAsync();

        var permissions = await _context.UserPermissions
            .Include(up => up.Page)
            .Where(up => up.UserId == userId)
            .ToListAsync();

        return BuildResponse(user, allPages, permissions);
    }

    public async Task<UserPermissionsResponse> UpdateAsync(int userId, UpdateUserPermissionsRequest request)
    {
        var user = await _context.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException($"Usuario {userId} no encontrado.");

        var pages = await _context.AppPages.ToListAsync();

        foreach (var (route, canAccess) in request.Pages)
        {
            var page = pages.FirstOrDefault(p =>
                string.Equals(p.Route, route, StringComparison.OrdinalIgnoreCase));
            if (page == null) continue;

            var existing = await _context.UserPermissions
                .FirstOrDefaultAsync(up => up.UserId == userId && up.PageId == page.Id);

            if (existing == null)
            {
                _context.UserPermissions.Add(new Core.Entities.UserPermission
                {
                    UserId = userId,
                    PageId = page.Id,
                    CanAccess = canAccess,
                    UpdatedAt = DateTimeOffset.UtcNow,
                });
            }
            else
            {
                existing.CanAccess = canAccess;
                existing.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        await _context.SaveChangesAsync();
        return await GetByUserIdAsync(userId);
    }

    public async Task InitializeAdminAsync(int userId)
    {
        var pages = await _context.AppPages.Where(p => p.IsActive).ToListAsync();

        foreach (var page in pages)
        {
            var exists = await _context.UserPermissions
                .AnyAsync(up => up.UserId == userId && up.PageId == page.Id);

            if (!exists)
            {
                _context.UserPermissions.Add(new Core.Entities.UserPermission
                {
                    UserId = userId,
                    PageId = page.Id,
                    CanAccess = true,
                    UpdatedAt = DateTimeOffset.UtcNow,
                });
            }
        }

        await _context.SaveChangesAsync();
    }

    // ── Helpers ────────────────────────────────────────────────
    private static UserPermissionsResponse BuildResponse(
        Core.Entities.User user,
        IEnumerable<Core.Entities.AppPage> allPages,
        IEnumerable<Core.Entities.UserPermission> permissions)
    {
        var permMap = permissions
            .Where(up => up.UserId == user.Id)
            .ToDictionary(up => up.Page?.Route ?? string.Empty, up => up.CanAccess);

        var pages = allPages.ToDictionary(
            p => p.Route,
            p => permMap.GetValueOrDefault(p.Route, false));

        return new UserPermissionsResponse
        {
            UserId   = user.Id,
            UserName = user.Name,
            Email    = user.Email,
            Role     = user.Role,
            IsActive = user.IsActive,
            Pages    = pages,
        };
    }
}

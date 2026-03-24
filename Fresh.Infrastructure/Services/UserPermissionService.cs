using Fresh.Core.DTOs.AppPage;
using Fresh.Core.DTOs.UserPermission;
using Fresh.Core.Entities;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fresh.Infrastructure.Services;

public class UserPermissionService : IUserPermissionService
{
    private readonly FreshDbContext _context;

    public UserPermissionService(FreshDbContext context) { _context = context; }

    public async Task<IEnumerable<UserPermissionResponse>> GetByUserIdAsync(int userId)
    {
        var permissions = await _context.UserPermissions
            .Include(up => up.Page)
            .Where(up => up.UserId == userId)
            .OrderBy(up => up.Page!.Name)
            .ToListAsync();

        return permissions.Select(MapToResponse);
    }

    public async Task<IEnumerable<AppPageResponse>> GetMenuForUserAsync(int userId)
    {
        var pages = await _context.UserPermissions
            .Include(up => up.Page)
            .Where(up => up.UserId == userId && up.CanAccess && up.Page!.IsActive)
            .OrderBy(up => up.Page!.Name)
            .Select(up => up.Page!)
            .ToListAsync();

        return pages.Select(p => new AppPageResponse
        {
            Id = p.Id,
            Name = p.Name,
            Route = p.Route,
            Icon = p.Icon,
            Description = p.Description,
            IsActive = p.IsActive
        });
    }

    public async Task<IEnumerable<UserPermissionResponse>> UpdateUserPermissionsAsync(int userId, IEnumerable<UserPermissionRequest> requests)
    {
        var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
        if (!userExists) throw new KeyNotFoundException("El usuario no existe.");

        var existingPermissions = await _context.UserPermissions
            .Where(up => up.UserId == userId)
            .ToListAsync();

        foreach (var req in requests)
        {
            var permission = existingPermissions.FirstOrDefault(p => p.PageId == req.PageId);

            if (permission == null)
            {
                _context.UserPermissions.Add(new UserPermission
                {
                    UserId = userId,
                    PageId = req.PageId,
                    CanAccess = req.CanAccess,
                    UpdatedAt = DateTimeOffset.UtcNow
                });
            }
            else
            {
                permission.CanAccess = req.CanAccess;
                permission.UpdatedAt = DateTimeOffset.UtcNow;
                _context.UserPermissions.Update(permission);
            }
        }

        await _context.SaveChangesAsync();
        return await GetByUserIdAsync(userId);
    }

    private static UserPermissionResponse MapToResponse(UserPermission up) => new()
    {
        Id = up.Id,
        UserId = up.UserId,
        PageId = up.PageId,
        PageName = up.Page?.Name ?? string.Empty,
        PageRoute = up.Page?.Route ?? string.Empty,
        PageIcon = up.Page?.Icon,
        CanAccess = up.CanAccess,
        UpdatedAt = up.UpdatedAt
    };
}
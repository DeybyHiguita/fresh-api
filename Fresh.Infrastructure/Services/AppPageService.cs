using Fresh.Core.DTOs.AppPage;
using Fresh.Core.Entities;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fresh.Infrastructure.Services;

public class AppPageService : IAppPageService
{
    private readonly FreshDbContext _context;

    public AppPageService(FreshDbContext context) { _context = context; }

    public async Task<IEnumerable<AppPageResponse>> GetAllAsync(bool onlyActive = true)
    {
        var query = _context.AppPages.AsQueryable();
        if (onlyActive) query = query.Where(p => p.IsActive);

        var pages = await query.OrderBy(p => p.Name).ToListAsync();
        return pages.Select(MapToResponse);
    }

    public async Task<AppPageResponse?> GetByIdAsync(int id)
    {
        var page = await _context.AppPages.FindAsync(id);
        return page == null ? null : MapToResponse(page);
    }

    public async Task<AppPageResponse> CreateAsync(AppPageRequest request)
    {
        var exists = await _context.AppPages.AnyAsync(p => p.Route.ToLower() == request.Route.ToLower());
        if (exists) throw new InvalidOperationException($"La ruta '{request.Route}' ya está registrada.");

        var page = new AppPage
        {
            Name = request.Name,
            Route = request.Route,
            Icon = request.Icon,
            Description = request.Description,
            IsActive = request.IsActive,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.AppPages.Add(page);
        await _context.SaveChangesAsync();
        return MapToResponse(page);
    }

    public async Task<AppPageResponse?> UpdateAsync(int id, AppPageRequest request)
    {
        var page = await _context.AppPages.FindAsync(id);
        if (page == null) return null;

        var exists = await _context.AppPages.AnyAsync(p => p.Route.ToLower() == request.Route.ToLower() && p.Id != id);
        if (exists) throw new InvalidOperationException($"La ruta '{request.Route}' ya está en uso por otra página.");

        page.Name = request.Name;
        page.Route = request.Route;
        page.Icon = request.Icon;
        page.Description = request.Description;
        page.IsActive = request.IsActive;

        _context.AppPages.Update(page);
        await _context.SaveChangesAsync();
        return MapToResponse(page);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var page = await _context.AppPages.FindAsync(id);
        if (page == null) return false;

        page.IsActive = false;
        _context.AppPages.Update(page);
        await _context.SaveChangesAsync();
        return true;
    }

    private static AppPageResponse MapToResponse(AppPage p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Route = p.Route,
        Icon = p.Icon,
        Description = p.Description,
        IsActive = p.IsActive,
        CreatedAt = p.CreatedAt
    };
}
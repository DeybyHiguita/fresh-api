using Fresh.Core.DTOs.User;
using Fresh.Core.Entities;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fresh.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly FreshDbContext _context;

    public UserService(FreshDbContext context) { _context = context; }

    public async Task<IEnumerable<UserResponse>> GetAllAsync(bool onlyActive = true)
    {
        var query = _context.Users.AsQueryable();
        if (onlyActive) query = query.Where(u => u.IsActive);

        var users = await query.OrderBy(u => u.Name).ToListAsync();
        return users.Select(MapToResponse);
    }

    public async Task<UserResponse?> GetByIdAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        return user == null ? null : MapToResponse(user);
    }

    public async Task<UserResponse?> UpdateAsync(int id, UserUpdateRequest request)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return null;

        user.Name = request.Name;
        user.Role = request.Role;
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Password))
            user.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);

        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        return MapToResponse(user);
    }

    private static UserResponse MapToResponse(User u) => new()
    {
        Id = u.Id,
        Name = u.Name,
        Email = u.Email,
        Role = u.Role,
        IsActive = u.IsActive,
        CreatedAt = new DateTimeOffset(u.CreatedAt, TimeSpan.Zero),
    };
}

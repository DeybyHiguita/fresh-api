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

    public async Task<IEnumerable<UserResponse>> GetUsersWithoutEmployeeAsync()
    {
        // Obtener mapa de usuarios que ya tienen empleado vinculado
        var employeeByUser = await _context.Employees
            .Where(e => e.UserId.HasValue)
            .ToDictionaryAsync(e => e.UserId!.Value, e => e.Id);

        // Obtener todos los usuarios activos
        var users = await _context.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name)
            .ToListAsync();

        // Retornar todos con indicador de si tienen empleado
        return users.Select(u => new UserResponse
        {
            Id = u.Id,
            Name = u.Name,
            Email = u.Email,
            Role = u.Role,
            IsActive = u.IsActive,
            CreatedAt = new DateTimeOffset(u.CreatedAt, TimeSpan.Zero),
            HasEmployee = employeeByUser.ContainsKey(u.Id),
            EmployeeId = employeeByUser.TryGetValue(u.Id, out var empId) ? empId : null
        });
    }

    public async Task<IEnumerable<UserResponse>> SearchByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Trim().Length < 2)
            return [];

        var search = email.Trim().ToLower();

        // Mapa de usuarios ya vinculados a un empleado
        var linkedUserIds = (await _context.Employees
            .Where(e => e.UserId.HasValue)
            .Select(e => e.UserId!.Value)
            .ToListAsync()).ToHashSet();

        var users = await _context.Users
            .Where(u => u.IsActive && u.Email.ToLower().Contains(search))
            .OrderBy(u => u.Email)
            .Take(20)
            .ToListAsync();

        return users.Select(u => new UserResponse
        {
            Id = u.Id,
            Name = u.Name,
            Email = u.Email,
            Role = u.Role,
            IsActive = u.IsActive,
            CreatedAt = new DateTimeOffset(u.CreatedAt, TimeSpan.Zero),
            HasEmployee = linkedUserIds.Contains(u.Id),
            EmployeeId = null
        });
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

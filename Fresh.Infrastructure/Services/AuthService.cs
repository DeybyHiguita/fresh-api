using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Fresh.Core.DTOs.Auth;
using Fresh.Core.DTOs.Store;
using Fresh.Core.Entities;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Fresh.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly FreshDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IAppSettingsService _appSettings;

    public AuthService(FreshDbContext context, IConfiguration configuration, IAppSettingsService appSettings)
    {
        _context = context;
        _configuration = configuration;
        _appSettings = appSettings;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users
            .Include(u => u.UserStores)
                .ThenInclude(us => us.Store)
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            throw new UnauthorizedAccessException("Credenciales inválidas.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Tu cuenta aún no ha sido activada por un administrador.");

        var stores = await BuildStoresListAsync(user);

        var defaultStore = stores.FirstOrDefault(s => s.IsDefault) ?? stores.FirstOrDefault();
        var activeStoreId = defaultStore?.Id ?? 0;

        return new AuthResponse
        {
            Id           = user.Id,
            Name         = user.Name,
            Email        = user.Email,
            Role         = user.Role,
            Token        = GenerateToken(user, activeStoreId),
            StoreId      = activeStoreId,
            IsSuperAdmin = user.IsSuperAdmin,
            Stores       = stores,
            Settings     = await _appSettings.GetAsync()
        };
    }

    public async Task RegisterAsync(RegisterRequest request)
    {
        var exists = await _context.Users.AnyAsync(u => u.Email == request.Email);
        if (exists)
            throw new InvalidOperationException("El correo ya está registrado.");

        var user = new User
        {
            Name     = request.Name,
            Email    = request.Email,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role     = "employee",
            IsActive = false
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }

    public async Task<AuthResponse> SwitchStoreAsync(int userId, int storeId)
    {
        var user = await _context.Users
            .Include(u => u.UserStores)
                .ThenInclude(us => us.Store)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            throw new UnauthorizedAccessException("Usuario no encontrado.");

        if (!user.IsSuperAdmin)
        {
            var hasAccess = user.UserStores.Any(us => us.StoreId == storeId && us.Store.IsActive);
            if (!hasAccess)
                throw new UnauthorizedAccessException("No tienes acceso a esta tienda.");
        }

        // storeId == 0 → superadmin en modo "todas las tiendas" (vista global)
        if (storeId != 0)
        {
            var storeExists = await _context.Stores.AnyAsync(s => s.Id == storeId && s.IsActive);
            if (!storeExists)
                throw new KeyNotFoundException("Tienda no encontrada o inactiva.");
        }
        else if (!user.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("No tienes acceso a la vista global.");
        }

        var stores = await BuildStoresListAsync(user);

        return new AuthResponse
        {
            Id           = user.Id,
            Name         = user.Name,
            Email        = user.Email,
            Role         = user.Role,
            Token        = GenerateToken(user, storeId),
            StoreId      = storeId,
            IsSuperAdmin = user.IsSuperAdmin,
            Stores       = stores,
            Settings     = await _appSettings.GetAsync()
        };
    }

    /// <summary>
    /// Lista de tiendas visibles para el usuario.
    /// Superadmin → todas las tiendas activas. Resto → solo las asignadas en user_stores.
    /// </summary>
    private async Task<List<StoreSummary>> BuildStoresListAsync(User user)
    {
        if (user.IsSuperAdmin)
        {
            var defaultId = user.UserStores.FirstOrDefault(us => us.IsDefault)?.StoreId;
            return await _context.Stores
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .Select(s => new StoreSummary { Id = s.Id, Name = s.Name, IsDefault = s.Id == defaultId })
                .ToListAsync();
        }

        return user.UserStores
            .Where(us => us.Store.IsActive)
            .Select(us => new StoreSummary { Id = us.StoreId, Name = us.Store.Name, IsDefault = us.IsDefault })
            .ToList();
    }

    private string GenerateToken(User user, int storeId)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Role, user.Role),
            new("store_id", storeId.ToString()),
            new("is_super_admin", user.IsSuperAdmin.ToString().ToLower())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

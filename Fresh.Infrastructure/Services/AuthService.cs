using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Fresh.Core.DTOs.Auth;
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
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            throw new UnauthorizedAccessException("Credenciales inválidas.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Tu cuenta aún no ha sido activada por un administrador.");

        return new AuthResponse
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            Token = GenerateToken(user),
            Settings = await _appSettings.GetAsync()
        };
    }

    public async Task RegisterAsync(RegisterRequest request)
    {
        var exists = await _context.Users.AnyAsync(u => u.Email == request.Email);
        if (exists)
            throw new InvalidOperationException("El correo ya está registrado.");

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "employee",
            IsActive = false
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }

    private string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role)
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

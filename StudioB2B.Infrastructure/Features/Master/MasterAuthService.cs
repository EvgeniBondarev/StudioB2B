using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using StudioB2B.Infrastructure.Persistence.Master;

namespace StudioB2B.Infrastructure.Features.Master;

public record MasterLoginRequest(string Email, string Password);

public record MasterAuthResult(
    bool Success,
    string? Token = null,
    DateTime? ExpiresAt = null,
    string? Error = null);

/// <summary>
/// Сервис авторизации пользователей в master-базе.
/// </summary>
public class MasterAuthService
{
    private readonly MasterDbContext _db;
    private readonly IConfiguration _configuration;

    public MasterAuthService(MasterDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public async Task<MasterAuthResult> LoginAsync(MasterLoginRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, ct);

        if (user is null)
            return Fail("Неверный email или пароль");

        if (!user.IsActive)
            return Fail("Пользователь деактивирован");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.HashPassword))
            return Fail("Неверный email или пароль");

        var roles = await _db.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == user.Id)
            .Join(_db.Roles.AsNoTracking(), ur => ur.RoleId, r => r.Id, (_, r) => r.Name)
            .ToListAsync(ct);

        var (token, expiresAt) = GenerateJwtToken(user.Id, user.Email, roles);
        return new MasterAuthResult(true, token, expiresAt);
    }

    private (string token, DateTime expiresAt) GenerateJwtToken(
        Guid userId, string email, IEnumerable<string> roles)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var secret = jwtSection["Secret"]!;
        var issuer = jwtSection["Issuer"] ?? "StudioB2B";
        var audience = jwtSection["Audience"] ?? "StudioB2B";
        var expiresMinutes = jwtSection.GetValue<int?>("ExpiresMinutes") ?? 60;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("scope", "master")
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var expiresAt = DateTime.UtcNow.AddMinutes(expiresMinutes);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    private static MasterAuthResult Fail(string error) => new(false, Error: error);
}


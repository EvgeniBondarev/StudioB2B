using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Persistence.Master;
using StudioB2B.Shared.DTOs;

namespace StudioB2B.Infrastructure.Services;


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

    public async Task<MasterAuthResultDto> LoginAsync(MasterLoginDto request, CancellationToken ct = default)
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

        var (token, expiresAt) = GenerateJwtToken(user, roles);
        return new MasterAuthResultDto(true, token, expiresAt);
    }

    private static readonly string[] UserRoleArray = { "User" };

    public async Task<MasterAuthResultDto> RegisterAsync(MasterRegisterDto request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            return Fail("Пользователь с таким email уже зарегистрирован");

        var userRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == "User", ct);
        if (userRole is null)
            return Fail("Роль 'User' не найдена. Обратитесь к администратору.");

        var user = new MasterUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            HashPassword = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            MiddleName = string.IsNullOrWhiteSpace(request.MiddleName) ? null : request.MiddleName.Trim(),
            IsActive = true
        };

        _db.Users.Add(user);
        _db.UserRoles.Add(new MasterUserRole { UserId = user.Id, RoleId = userRole.Id });
        await _db.SaveChangesAsync(ct);

        var (token, expiresAt) = GenerateJwtToken(user, UserRoleArray);
        return new MasterAuthResultDto(true, token, expiresAt);
    }

    private (string token, DateTime expiresAt) GenerateJwtToken(MasterUser user, IEnumerable<string> roles)
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
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new(JwtRegisteredClaimNames.FamilyName, user.LastName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("scope", "master")
        };

        if (!string.IsNullOrWhiteSpace(user.MiddleName))
            claims.Add(new Claim("middle_name", user.MiddleName));

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

    private static MasterAuthResultDto Fail(string error) => new MasterAuthResultDto(false, Error: error);
}


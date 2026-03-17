using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Infrastructure.Services;
using StudioB2B.Shared.DTOs;

namespace StudioB2B.Web.Controllers;

[Route("api/auth")]
[ApiController]
public class AccountController : ControllerBase
{
    private readonly ITenantProvider _tenantProvider;
    private readonly ITenantDbContextFactory _dbContextFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AccountController> _logger;

    public AccountController(ITenantProvider tenantProvider, ITenantDbContextFactory dbContextFactory,
                             IConfiguration configuration, ILogger<AccountController> logger)
    {
        _tenantProvider = tenantProvider;
        _dbContextFactory = dbContextFactory;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Авторизация: возвращает JWT-токен
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto request, CancellationToken ct = default)
    {
        if (!_tenantProvider.IsResolved)
            return BadRequest(new { error = "Tenant not resolved" });

        await using var db = _dbContextFactory.CreateDbContext();

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, ct);

        if (user is null)
        {
            _logger.LogWarning("Login failed: user {Email} not found", email);
            return Unauthorized(new { error = "Неверный email или пароль" });
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login failed: user {Email} is inactive", email);
            return Unauthorized(new { error = "Пользователь деактивирован" });
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.HashPassword))
        {
            _logger.LogWarning("Login failed: invalid password for {Email}", email);
            return Unauthorized(new { error = "Неверный email или пароль" });
        }

        var roles = await db.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == user.Id)
            .Join(db.Roles.AsNoTracking(), ur => ur.RoleId, r => r.Id, (_, r) => r.Name)
            .ToListAsync(ct);

        var token = GenerateJwtToken(user.Id, user.Email, roles);
        var expiresMinutes = _configuration.GetSection("Jwt").GetValue<int?>("ExpiresMinutes") ?? 60;

        _logger.LogInformation("User {Email} logged in successfully", email);

        return Ok(new
        {
            token,
            expiresAt = DateTime.UtcNow.AddMinutes(expiresMinutes)
        });
    }

    /// <summary>
    /// Перевыпускает JWT с актуальными ролями текущего пользователя.
    /// Вызывается клиентом сразу после изменения ролей, чтобы не требовать повторного входа.
    /// </summary>
    [HttpGet("refresh")]
    [Authorize]
    public async Task<IActionResult> Refresh(CancellationToken ct = default)
    {
        if (!_tenantProvider.IsResolved)
            return BadRequest(new { error = "Tenant not resolved" });

        var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                       ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(subClaim, out var userId))
            return Unauthorized(new { error = "Invalid token" });

        await using var db = _dbContextFactory.CreateDbContext();

        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive && !u.IsDeleted, ct);

        if (user is null)
            return Unauthorized(new { error = "User not found or inactive" });

        var roles = await db.UserRoles.AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Join(db.Roles.AsNoTracking(), ur => ur.RoleId, r => r.Id, (_, r) => r.Name)
            .ToListAsync(ct);

        var token = GenerateJwtToken(user.Id, user.Email, roles);
        var expiresMinutes = _configuration.GetSection("Jwt").GetValue<int?>("ExpiresMinutes") ?? 60;

        return Ok(new
        {
            token,
            expiresAt = DateTime.UtcNow.AddMinutes(expiresMinutes)
        });
    }

    private string GenerateJwtToken(Guid userId, string email, IEnumerable<string> roles)
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
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

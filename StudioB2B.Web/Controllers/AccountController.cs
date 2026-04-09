using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Shared;

namespace StudioB2B.Web.Controllers;

[Route("api/auth")]
[ApiController]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IAccountService accountService,
        ITenantProvider tenantProvider,
        IConfiguration configuration,
        ILogger<AccountController> logger)
    {
        _accountService = accountService;
        _tenantProvider = tenantProvider;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Шаг 1 входа: проверяет учётные данные и отправляет код на email.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto request, CancellationToken ct = default)
    {
        if (!_tenantProvider.IsResolved)
            return BadRequest(new { error = "Tenant not resolved" });

        var result = await _accountService.InitiateLoginAsync(request.Email, request.Password, ct);

        if (result is null)
        {
            _logger.LogWarning("Login failed for {Email}", request.Email);
            return Unauthorized(new { error = "Неверный email или пароль" });
        }

        _logger.LogInformation("Login code sent to {Email}", request.Email);
        return Ok(new { requiresVerification = true });
    }

    /// <summary>
    /// Шаг 2 входа: подтверждает код и возвращает JWT-токен.
    /// </summary>
    [HttpPost("verify-login")]
    public async Task<IActionResult> VerifyLogin([FromBody] VerifyLoginRequest request, CancellationToken ct = default)
    {
        if (!_tenantProvider.IsResolved)
            return BadRequest(new { error = "Tenant not resolved" });

        var result = await _accountService.VerifyLoginCodeAsync(request.Email, request.Code, ct);

        if (result is null)
        {
            _logger.LogWarning("Login code verification failed for {Email}", request.Email);
            return BadRequest(new { error = "Неверный или устаревший код. Запросите новый." });
        }

        var token = GenerateJwtToken(result.UserId, result.Email, result.IsFullAccess, result.RoleNames);
        var expiresMinutes = _configuration.GetSection("Jwt").GetValue<int?>("ExpiresMinutes") ?? 60;

        _logger.LogInformation("User {Email} logged in successfully", result.Email);
        return Ok(new { token, expiresAt = DateTime.UtcNow.AddMinutes(expiresMinutes) });
    }

    /// <summary>
    /// Повторная отправка кода входа.
    /// </summary>
    [HttpPost("resend-login-code")]
    public async Task<IActionResult> ResendLoginCode([FromBody] ResendLoginCodeRequest request, CancellationToken ct = default)
    {
        if (!_tenantProvider.IsResolved)
            return BadRequest(new { error = "Tenant not resolved" });

        var ok = await _accountService.ResendLoginCodeAsync(request.Email, ct);

        if (!ok)
        {
            _logger.LogWarning("Resend login code failed for {Email}", request.Email);
            return BadRequest(new { error = "Пользователь не найден" });
        }

        _logger.LogInformation("Login code resent to {Email}", request.Email);
        return Ok(new { requiresVerification = true });
    }

    /// <summary>
    /// Перевыпускает JWT с актуальными ролями текущего пользователя.
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

        var result = await _accountService.RefreshAsync(userId, ct);

        if (result is null)
            return Unauthorized(new { error = "User not found or inactive" });

        var token = GenerateJwtToken(result.UserId, result.Email, result.IsFullAccess, result.RoleNames);
        var expiresMinutes = _configuration.GetSection("Jwt").GetValue<int?>("ExpiresMinutes") ?? 60;

        return Ok(new { token, expiresAt = DateTime.UtcNow.AddMinutes(expiresMinutes) });
    }

    private string GenerateJwtToken(Guid userId, string email, bool isFullAccess, IEnumerable<string> roleNames)
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

        if (isFullAccess)
            claims.Add(new Claim("full_access", "true"));

        foreach (var role in roleNames)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public record VerifyLoginRequest(string Email, string Code);

    public record ResendLoginCodeRequest(string Email);
}

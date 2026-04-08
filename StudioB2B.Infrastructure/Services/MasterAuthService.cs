using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Infrastructure.Persistence.Master;
using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Services;

/// <summary>
/// Сервис авторизации пользователей в master-базе.
/// </summary>
public class MasterAuthService
{
    private readonly MasterDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly ILogger<MasterAuthService> _logger;

    public MasterAuthService(MasterDbContext db, IConfiguration configuration, IEmailService emailService, ILogger<MasterAuthService> logger)
    {
        _db = db;
        _configuration = configuration;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<MasterAuthResultDto> LoginAsync(MasterLoginDto request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, ct);

        if (user is null)
            return Fail("Неверный email или пароль");

        if (!user.IsEmailVerified)
            return Fail("Email не подтверждён. Завершите регистрацию, введя код из письма.");

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

    /// <summary>
    /// Шаг 1: создаёт пользователя (неактивного) и отправляет код на email.
    /// Если пользователь с таким email уже есть и ещё не подтверждён — код перевыпускается.
    /// </summary>
    public async Task<MasterRegisterResultDto> RegisterAsync(MasterRegisterDto request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var existing = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        if (existing is not null && existing.IsEmailVerified)
            return new MasterRegisterResultDto(false, Error: "Пользователь с таким email уже зарегистрирован");

        if (existing is null)
        {
            var userRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == "User", ct);
            if (userRole is null)
                return new MasterRegisterResultDto(false, Error: "Роль 'User' не найдена. Обратитесь к администратору.");

            var user = new MasterUser
            {
                Id = Guid.NewGuid(),
                Email = email,
                HashPassword = BCrypt.Net.BCrypt.HashPassword(request.Password),
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                MiddleName = string.IsNullOrWhiteSpace(request.MiddleName) ? null : request.MiddleName.Trim(),
                IsActive = false,
                IsEmailVerified = false
            };

            _db.Users.Add(user);
            _db.UserRoles.Add(new MasterUserRole { UserId = user.Id, RoleId = userRole.Id });
        }
        else
        {
            // Обновляем данные и пароль при повторной попытке
            existing.HashPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
            existing.FirstName = request.FirstName.Trim();
            existing.LastName = request.LastName.Trim();
            existing.MiddleName = string.IsNullOrWhiteSpace(request.MiddleName) ? null : request.MiddleName.Trim();
        }

        var code = await PrepareVerificationCodeAsync(email, ct);
        await _db.SaveChangesAsync(ct);

        _ = SendEmailFireAndForgetAsync(email, code);

        return new MasterRegisterResultDto(true, RequiresVerification: true);
    }

    /// <summary>
    /// Повторная отправка кода (без изменения данных пользователя).
    /// </summary>
    public async Task<MasterRegisterResultDto> ResendCodeAsync(string email, CancellationToken ct = default)
    {
        email = email.Trim().ToLowerInvariant();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        if (user is null)
            return new MasterRegisterResultDto(false, Error: "Пользователь не найден");

        if (user.IsEmailVerified)
            return new MasterRegisterResultDto(false, Error: "Email уже подтверждён");

        var code = await PrepareVerificationCodeAsync(email, ct);
        await _db.SaveChangesAsync(ct);

        _ = SendEmailFireAndForgetAsync(email, code);

        return new MasterRegisterResultDto(true, RequiresVerification: true);
    }

    /// <summary>
    /// Шаг 2: проверяет код, активирует пользователя, возвращает JWT.
    /// </summary>
    public async Task<MasterAuthResultDto> VerifyEmailAsync(MasterVerifyEmailDto request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var code = await _db.EmailVerificationCodes
            .Where(c => c.Email == email && !c.IsUsed && c.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(c => c.ExpiresAt)
            .FirstOrDefaultAsync(ct);

        if (code is null)
            return Fail("Неверный или устаревший код. Запросите новый.");

        if (code.Code != request.Code.Trim())
            return Fail("Неверный код подтверждения");

        code.IsUsed = true;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null)
            return Fail("Пользователь не найден");

        user.IsEmailVerified = true;
        user.IsActive = true;

        await _db.SaveChangesAsync(ct);

        var roles = await _db.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == user.Id)
            .Join(_db.Roles.AsNoTracking(), ur => ur.RoleId, r => r.Id, (_, r) => r.Name)
            .ToListAsync(ct);

        var (token, expiresAt) = GenerateJwtToken(user, roles);
        return new MasterAuthResultDto(true, token, expiresAt);
    }

    private async Task<string> PrepareVerificationCodeAsync(string email, CancellationToken ct)
    {
        var oldCodes = await _db.EmailVerificationCodes
            .Where(c => c.Email == email && !c.IsUsed)
            .ToListAsync(ct);

        foreach (var old in oldCodes)
            old.IsUsed = true;

        var code = new EmailVerificationCode
        {
            Id = Guid.NewGuid(),
            Email = email,
            Code = GenerateCode(),
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            IsUsed = false
        };

        _db.EmailVerificationCodes.Add(code);

        _logger.LogInformation("Email verification code for {Email}: {Code}", email, code.Code);

        return code.Code;
    }

    private async Task SendEmailFireAndForgetAsync(string email, string code)
    {
        try
        {
            var html = $"""
                <div style="font-family:sans-serif;max-width:480px;margin:0 auto">
                  <h2>Подтверждение email</h2>
                  <p>Ваш код подтверждения для регистрации в <strong>StudioB2B</strong>:</p>
                  <div style="font-size:2rem;font-weight:700;letter-spacing:0.3em;padding:1rem 1.5rem;background:#f5f5f5;border-radius:8px;display:inline-block">{code}</div>
                  <p style="color:#888;font-size:0.875rem">Код действителен 15 минут.</p>
                </div>
                """;

            await _emailService.SendAsync(email, email, "Код подтверждения — StudioB2B", html);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification email to {Email}", email);
        }
    }

    private static string GenerateCode()
    {
        return Random.Shared.Next(100_000, 999_999).ToString();
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

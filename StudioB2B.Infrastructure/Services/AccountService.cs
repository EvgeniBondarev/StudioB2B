using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Infrastructure.Persistence.Tenant;
using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Services;

public class AccountService : IAccountService
{
    private readonly ITenantDbContextFactory _dbContextFactory;
    private readonly IEmailService _emailService;
    private readonly ILogger<AccountService> _logger;

    public AccountService(ITenantDbContextFactory dbContextFactory, IEmailService emailService, ILogger<AccountService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _emailService = emailService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<TenantLoginInitResultDto?> InitiateLoginAsync(string email, string password, CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);

        if (user is null || !user.IsActive)
            return null;

        if (!BCrypt.Net.BCrypt.Verify(password, user.HashPassword))
            return null;

        var code = await PrepareLoginCodeAsync(db, user.Id, ct);
        await db.SaveChangesAsync(ct);

        _ = SendLoginEmailFireAndForgetAsync(normalizedEmail, code);

        return new TenantLoginInitResultDto(RequiresVerification: true);
    }

    /// <inheritdoc/>
    public async Task<AccountLoginResultDto?> VerifyLoginCodeAsync(string email, string code, CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);

        if (user is null || !user.IsActive)
            return null;

        var loginCode = await db.LoginCodes
            .Where(c => c.UserId == user.Id && !c.IsUsed && c.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(c => c.ExpiresAt)
            .FirstOrDefaultAsync(ct);

        if (loginCode is null || loginCode.Code != code.Trim())
            return null;

        loginCode.IsUsed = true;
        await db.SaveChangesAsync(ct);

        var (isFullAccess, roleNames) = await BuildRoleClaimsAsync(db, user.Id, ct);
        return new AccountLoginResultDto
        {
            UserId = user.Id,
            Email = user.Email,
            IsFullAccess = isFullAccess,
            RoleNames = roleNames
        };
    }

    /// <inheritdoc/>
    public async Task<bool> ResendLoginCodeAsync(string email, CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);

        if (user is null || !user.IsActive)
            return false;

        var code = await PrepareLoginCodeAsync(db, user.Id, ct);
        await db.SaveChangesAsync(ct);

        _ = SendLoginEmailFireAndForgetAsync(normalizedEmail, code);

        return true;
    }

    /// <inheritdoc/>
    public async Task<AccountLoginResultDto?> RefreshAsync(Guid userId, CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();

        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive && !u.IsDeleted, ct);

        if (user is null)
            return null;

        var (isFullAccess, roleNames) = await BuildRoleClaimsAsync(db, userId, ct);
        return new AccountLoginResultDto
        {
            UserId = user.Id,
            Email = user.Email,
            IsFullAccess = isFullAccess,
            RoleNames = roleNames
        };
    }

    private static async Task<string> PrepareLoginCodeAsync(TenantDbContext db, Guid userId, CancellationToken ct)
    {
        var oldCodes = await db.LoginCodes
            .Where(c => c.UserId == userId && !c.IsUsed)
            .ToListAsync(ct);

        foreach (var old in oldCodes)
            old.IsUsed = true;

        var code = new TenantLoginCode
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Code = Random.Shared.Next(100_000, 999_999).ToString(),
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            IsUsed = false
        };

        db.LoginCodes.Add(code);
        return code.Code;
    }

    private async Task SendLoginEmailFireAndForgetAsync(string email, string code)
    {
        _logger.LogInformation("Tenant login verification code for {Email}: {Code}", email, code);

        try
        {
            var html = $"""
                <!DOCTYPE html>
                <html lang="ru">
                <head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
                <body style="margin:0;padding:0;background:#f4f4f7;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
                  <table width="100%" cellpadding="0" cellspacing="0" role="presentation">
                    <tr>
                      <td align="center" style="padding:48px 16px;">
                        <table width="480" cellpadding="0" cellspacing="0" role="presentation" style="max-width:480px;width:100%;background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.08);">
                          <tr>
                            <td style="background:#4f46e5;padding:32px 40px;text-align:center;">
                              <span style="color:#ffffff;font-size:24px;font-weight:700;letter-spacing:1px;">StudioB2B</span>
                            </td>
                          </tr>
                          <tr>
                            <td style="padding:40px 40px 32px;">
                              <h1 style="margin:0 0 12px;font-size:20px;font-weight:700;color:#111827;">Код входа</h1>
                              <p style="margin:0 0 28px;font-size:15px;color:#4b5563;line-height:1.6;">
                                Введите этот код для входа в <strong style="color:#111827;">StudioB2B</strong>:
                              </p>
                              <div style="text-align:center;margin:0 0 32px;">
                                <span style="display:inline-block;font-size:38px;font-weight:800;letter-spacing:0.5em;font-family:'Courier New',Courier,monospace;background:#f0f0ff;border:2px solid #c7c5f0;border-radius:10px;padding:16px 24px 16px 32px;color:#4f46e5;">{code}</span>
                              </div>
                              <p style="margin:0;font-size:13px;color:#9ca3af;line-height:1.6;">
                                ⏱&nbsp; Код действителен <strong>15 минут</strong>.<br>
                                Если вы не запрашивали этот код — немедленно смените пароль в настройках аккаунта.
                              </p>
                            </td>
                          </tr>
                          <tr>
                            <td style="background:#f8f8fc;border-top:1px solid #e8e8f0;padding:20px 40px;text-align:center;">
                              <p style="margin:0;font-size:12px;color:#9ca3af;">StudioB2B &nbsp;·&nbsp; Это автоматическое сообщение, не отвечайте на него.</p>
                            </td>
                          </tr>
                        </table>
                      </td>
                    </tr>
                  </table>
                </body>
                </html>
                """;

            await _emailService.SendAsync(email, email, "Код входа — StudioB2B", html);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send login code email to {Email}", email);
        }
    }

    private static async Task<(bool isFullAccess, IEnumerable<string> roleNames)> BuildRoleClaimsAsync(
        TenantDbContext db, Guid userId, CancellationToken ct)
    {
        var userPermissions = await db.UserPermissions
            .AsNoTracking()
            .Where(up => up.UserId == userId)
            .Include(up => up.Permission)
                .ThenInclude(p => p.Pages)
                    .ThenInclude(pp => pp.Page)
            .Include(up => up.Permission)
                .ThenInclude(p => p.Functions)
                    .ThenInclude(pf => pf.Function)
            .Include(up => up.Permission)
                .ThenInclude(p => p.PageColumns)
                    .ThenInclude(pc => pc.PageColumn)
            .ToListAsync(ct);

        bool isFullAccess = userPermissions.Any(up => up.Permission.IsFullAccess);
        if (isFullAccess)
            return (true, ["Admin"]);

        var roleNames = userPermissions
            .SelectMany(up => up.Permission.Pages.Select(pp => pp.Page.Name))
            .Concat(userPermissions.SelectMany(up => up.Permission.Functions.Select(pf => pf.Function.Name)))
            .Concat(userPermissions.SelectMany(up => up.Permission.PageColumns.Select(pc => pc.PageColumn.Name)))
            .Distinct()
            .ToList();

        return (false, roleNames);
    }
}

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
                <div style="font-family:sans-serif;max-width:480px;margin:0 auto">
                  <h2>Код входа</h2>
                  <p>Ваш код для входа в <strong>StudioB2B</strong>:</p>
                  <div style="font-size:2rem;font-weight:700;letter-spacing:0.3em;padding:1rem 1.5rem;background:#f5f5f5;border-radius:8px;display:inline-block">{code}</div>
                  <p style="color:#888;font-size:0.875rem">Код действителен 15 минут.</p>
                </div>
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

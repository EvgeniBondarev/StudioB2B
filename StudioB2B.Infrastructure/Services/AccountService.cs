using Microsoft.EntityFrameworkCore;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Infrastructure.Persistence.Tenant;
using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Services;

public class AccountService : IAccountService
{
    private readonly ITenantDbContextFactory _dbContextFactory;

    public AccountService(ITenantDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    /// <inheritdoc/>
    public async Task<AccountLoginResultDto?> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);

        if (user is null || !user.IsActive)
            return null;

        if (!BCrypt.Net.BCrypt.Verify(password, user.HashPassword))
            return null;

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


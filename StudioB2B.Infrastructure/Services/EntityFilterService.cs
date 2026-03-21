using Microsoft.EntityFrameworkCore;
using StudioB2B.Domain.Constants;
using StudioB2B.Infrastructure.Interfaces;

namespace StudioB2B.Infrastructure.Services;

/// <summary>
/// Resolves the current user's BlockedEntity restrictions.
/// The result is cached per user-id so it is rebuilt if the user changes.
/// Call <see cref="InvalidateCache"/> after any permission change to force a reload.
/// </summary>
public class EntityFilterService : IEntityFilterService
{
    private readonly ITenantDbContextFactory _factory;
    private readonly ICurrentUserProvider _currentUser;

    // Cache keyed by userId — null means "not yet built for this user"
    private Guid? _cachedUserId;
    private Dictionary<BlockedEntityTypeEnum, HashSet<Guid>?>? _cache;

    public EntityFilterService(ITenantDbContextFactory factory, ICurrentUserProvider currentUser)
    {
        _factory = factory;
        _currentUser = currentUser;
    }

    /// <summary>Clears the cached restrictions so they are reloaded on the next access.</summary>
    public void InvalidateCache()
    {
        _cache = null;
        _cachedUserId = null;
    }

    public async Task<HashSet<Guid>?> GetAllowedIdsAsync(
        BlockedEntityTypeEnum entityType, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;

        // If the user is not identified yet, don't cache — return "unrestricted" for now.
        // Once UserContext is populated and the page reloads data, filtering will be applied.
        if (userId is null)
            return null;

        // Rebuild cache whenever userId changes (e.g., after login or UserContext init)
        if (_cache is null || _cachedUserId != userId)
        {
            _cachedUserId = userId;
            _cache = await BuildCacheAsync(userId.Value, ct);
        }

        return _cache.TryGetValue(entityType, out var ids) ? ids : null;
    }

    private async Task<Dictionary<BlockedEntityTypeEnum, HashSet<Guid>?>> BuildCacheAsync(
        Guid userId, CancellationToken ct)
    {
        using var db = _factory.CreateDbContext();

        var permissionIds = await db.UserPermissions
            .AsNoTracking()
            .Where(up => up.UserId == userId)
            .Select(up => up.PermissionId)
            .ToListAsync(ct);

        // Full-access permission → no restrictions
        var hasFullAccess = await db.Permissions
            .AsNoTracking()
            .AnyAsync(p => permissionIds.Contains(p.Id) && p.IsFullAccess, ct);

        if (hasFullAccess)
            return new Dictionary<BlockedEntityTypeEnum, HashSet<Guid>?>();

        var blockedEntities = await db.BlockedEntities
            .AsNoTracking()
            .Where(be => permissionIds.Contains(be.PermissionId))
            .ToListAsync(ct);

        var result = new Dictionary<BlockedEntityTypeEnum, HashSet<Guid>?>();
        foreach (var entityType in Enum.GetValues<BlockedEntityTypeEnum>())
        {
            var allowed = blockedEntities
                .Where(be => be.EntityType == entityType)
                .Select(be => be.EntityId)
                .ToHashSet();

            // Empty = unrestricted (null). Non-empty = whitelist.
            result[entityType] = allowed.Count > 0 ? allowed : null;
        }

        return result;
    }
}


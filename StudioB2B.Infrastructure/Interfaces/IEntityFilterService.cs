using StudioB2B.Domain.Constants;

namespace StudioB2B.Infrastructure.Interfaces;

/// <summary>
/// Resolves which entity IDs are accessible for the current user based on BlockedEntity restrictions.
/// Returns <c>null</c> when there are no restrictions (all IDs accessible).
/// Returns a <see cref="HashSet{T}"/> of allowed IDs when restrictions are configured.
/// </summary>
public interface IEntityFilterService
{
    /// <summary>
    /// Gets the set of allowed entity IDs for the given type, or <c>null</c> if unrestricted.
    /// </summary>
    Task<HashSet<Guid>?> GetAllowedIdsAsync(BlockedEntityTypeEnum entityType, CancellationToken ct = default);

    /// <summary>
    /// Clears the cached restrictions so they are reloaded on the next access.
    /// Call this after updating a user's permissions.
    /// </summary>
    void InvalidateCache();
}


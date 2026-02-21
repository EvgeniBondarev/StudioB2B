using StudioB2B.Application.Common.Interfaces;
using StudioB2B.Infrastructure.BackgroundJobs;

namespace StudioB2B.Infrastructure.BackgroundJobs;

/// <summary>
/// Реализация IRoleSyncPublisher — пишет задание в RoleSyncChannel
/// </summary>
public class RoleSyncPublisher(RoleSyncChannel channel) : IRoleSyncPublisher
{
    public void Publish(Guid roleId, string roleName, string? description, bool isSystemRole, RoleSyncAction action)
    {
        var operation = action == RoleSyncAction.Delete
            ? RoleSyncOperation.Delete
            : RoleSyncOperation.Upsert;

        channel.Publish(new RoleSyncJob(
            roleId,
            roleName,
            roleName.ToUpperInvariant(),
            description,
            isSystemRole,
            operation));
    }
}


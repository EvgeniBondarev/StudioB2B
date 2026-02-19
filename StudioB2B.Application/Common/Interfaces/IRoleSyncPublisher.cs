namespace StudioB2B.Application.Common.Interfaces;

public enum RoleSyncAction { Upsert, Delete }

/// <summary>
/// Публикует задания синхронизации ролей в фоновый воркер
/// </summary>
public interface IRoleSyncPublisher
{
    void Publish(Guid roleId, string roleName, string? description, bool isSystemRole, RoleSyncAction action);
}

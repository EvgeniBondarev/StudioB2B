using StudioB2B.Shared.DTOs;

namespace StudioB2B.Infrastructure.Interfaces;

/// <summary>
/// Сервис для управления мастер-пользователями и их ролями.
/// Работает с основной (master) базой данных, не с тенантной.
/// </summary>
public interface IMasterUserService
{
    /// <summary>
    /// Загружает всё необходимое для страницы: пользователей, роли, связи.
    /// </summary>
    Task<MasterUserInitData> GetInitDataAsync(CancellationToken ct = default);

    /// <summary>
    /// Атомарно обновляет роли пользователя: добавляет toAdd, удаляет toRemove.
    /// </summary>
    Task UpdateUserRolesAsync(
        Guid userId,
        IEnumerable<Guid> toAdd,
        IEnumerable<Guid> toRemove,
        CancellationToken ct = default);

    /// <summary>
    /// Устанавливает флаг IsActive для пользователя.
    /// </summary>
    Task SetUserActiveAsync(Guid userId, bool isActive, CancellationToken ct = default);
}


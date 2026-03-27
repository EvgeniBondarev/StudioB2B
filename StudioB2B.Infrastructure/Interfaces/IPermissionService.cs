using StudioB2B.Shared.DTOs;

namespace StudioB2B.Infrastructure.Interfaces;

/// <summary>
/// Сервис для работы с правами доступа тенанта.
/// Инкапсулирует все запросы к БД и скрывает DbContext от слоя UI.
/// </summary>
public interface IPermissionService
{
    /// <summary>Список всех прав (для страницы-грида).</summary>
    Task<List<PermissionDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Плоский список прав — Id/Name — для выпадающего списка.</summary>
    Task<List<LabelValueDto>> GetAvailableAsync(CancellationToken ct = default);

    /// <summary>Право по Id со всеми связями (для диалога редактирования).</summary>
    Task<PermissionDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Страницы со своими колонками и функциями (для диалога редактирования).</summary>
    Task<List<PageWithDetailsDto>> GetPagesWithDetailsAsync(CancellationToken ct = default);

    /// <summary>Варианты сущностей для каждого типа ограничения (для диалога редактирования).</summary>
    Task<Dictionary<string, List<EntityOptionDto>>> GetEntityOptionsAsync(CancellationToken ct = default);

    /// <summary>Создать новое право.</summary>
    Task<(bool Success, string? Error, Guid Id)> CreateAsync(CreatePermissionDto dto, CancellationToken ct = default);

    /// <summary>Обновить существующее право.</summary>
    Task<(bool Success, string? Error)> UpdateAsync(Guid id, UpdatePermissionDto dto, CancellationToken ct = default);

    /// <summary>Мягкое удаление права (снимается со всех пользователей).</summary>
    Task<(bool Success, string? Error)> DeleteAsync(Guid id, CancellationToken ct = default);
}


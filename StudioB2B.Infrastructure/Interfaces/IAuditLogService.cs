using StudioB2B.Domain.Entities;
using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Interfaces;

/// <summary>
/// Сервис для работы с журналом изменений (аудитом) тенанта.
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Получить список уникальных типов сущностей для фильтра.
    /// </summary>
    Task<List<string>> GetFilterEntityNamesAsync(CancellationToken ct = default);

    /// <summary>
    /// Получить список уникальных пользователей для фильтра.
    /// </summary>
    Task<List<string>> GetFilterUserNamesAsync(CancellationToken ct = default);

    /// <summary>
    /// Получить страницу записей аудита с учётом фильтров и сортировки.
    /// </summary>
    Task<(List<FieldAuditLog> Items, int Total)> GetPagedAsync(
        AuditLogFilter filter,
        int skip,
        int take,
        string? orderBy,
        CancellationToken ct = default);

    /// <summary>
    /// Получить все записи аудита для конкретной сущности (используется в FieldAuditDialog).
    /// </summary>
    Task<List<FieldAuditLog>> GetByEntityAsync(
        string entityName,
        string entityId,
        CancellationToken ct = default);

    /// <summary>
    /// Получить записи аудита для нескольких субъектов (используется в AuditHistoryDialog).
    /// </summary>
    Task<List<FieldAuditLog>> GetBySubjectsAsync(
        IReadOnlyList<AuditSubject> subjects,
        CancellationToken ct = default);

    /// <summary>
    /// Построить словарь GUID → отображаемое имя для FK-полей в журнале.
    /// </summary>
    Task<IReadOnlyDictionary<string, string>> BuildValueResolverAsync(
        List<FieldAuditLog> logs,
        CancellationToken ct = default);
}


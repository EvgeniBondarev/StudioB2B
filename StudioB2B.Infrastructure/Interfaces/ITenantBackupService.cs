using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Interfaces;

/// <summary>
/// Сервис управления бэкапами баз данных тенантов.
/// Доступен только Admin-пользователям мастер-панели.
/// </summary>
public interface ITenantBackupService
{
    /// <summary>Возвращает расписание бэкапов тенанта (null если не настроено).</summary>
    Task<TenantBackupScheduleDto?> GetScheduleAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>Создаёт или обновляет расписание и регистрирует recurring job в Hangfire.</summary>
    Task<TenantBackupScheduleDto> SaveScheduleAsync(SaveTenantBackupScheduleDto dto, CancellationToken ct = default);

    /// <summary>Удаляет расписание и отменяет recurring job в Hangfire.</summary>
    Task DeleteScheduleAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>Ставит бэкап в очередь немедленно.</summary>
    Task TriggerBackupNowAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>Возвращает последние записи истории бэкапов тенанта.</summary>
    Task<List<TenantBackupHistoryDto>> GetHistoryAsync(Guid tenantId, int limit = 10, CancellationToken ct = default);

    /// <summary>Генерирует presigned URL для скачивания файла бэкапа (TTL 15 мин).</summary>
    Task<string> GetDownloadUrlAsync(Guid historyId, CancellationToken ct = default);
}


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

    /// <summary>
    /// Создаёт одноразовый токен для скачивания бэкапа (TTL 15 мин).
    /// Токен не требует Bearer-аутентификации — он сам является доказательством доступа.
    /// </summary>
    Task<string> CreateDownloadTokenAsync(Guid historyId, CancellationToken ct = default);

    /// <summary>
    /// Проверяет и удаляет одноразовый токен из кэша.
    /// Возвращает null если токен недействителен или истёк.
    /// </summary>
    (string ObjectKey, string FileName, long? SizeBytes)? ConsumeDownloadToken(string token);

    /// <summary>Стримит объект MinIO напрямую в output.</summary>
    Task StreamObjectAsync(string objectKey, Stream output, CancellationToken ct = default);

    /// <summary>Загружает поток данных в MinIO по указанному ключу (без буферизации на диск).</summary>
    Task UploadToMinioAsync(string objectKey, Stream body, long? size, CancellationToken ct = default);

    /// <summary>Ставит задачу восстановления из сохранённого бэкапа в очередь Hangfire.</summary>
    Task EnqueueRestoreAsync(Guid tenantId, Guid historyId, CancellationToken ct = default);

    /// <summary>Ставит задачу восстановления из загруженного файла в очередь Hangfire.</summary>
    Task EnqueueRestoreAsync(Guid tenantId, string objectKey, string sourceType, CancellationToken ct = default);

    /// <summary>Возвращает последние записи истории восстановлений тенанта.</summary>
    Task<List<TenantRestoreHistoryDto>> GetRestoreHistoryAsync(Guid tenantId, int limit = 10, CancellationToken ct = default);
}


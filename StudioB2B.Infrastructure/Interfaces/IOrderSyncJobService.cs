using StudioB2B.Domain.Entities.Orders;

namespace StudioB2B.Infrastructure.Interfaces;

/// <summary>
/// Управляет постановкой и отменой фоновых задач синхронизации заказов (Hangfire).
/// </summary>
public interface IOrderSyncJobService
{
    /// <summary>Ставит в очередь задачу загрузки заказов за период. Возвращает Id записи истории.</summary>
    Task<Guid> EnqueueSyncAsync(DateTime from, DateTime to);

    /// <summary>Ставит в очередь задачу обновления статусов. Возвращает Id записи истории.</summary>
    Task<Guid> EnqueueUpdateAsync();

    /// <summary>Отменяет задачу (Delete в Hangfire + Status = Cancelled в истории).</summary>
    Task CancelJobAsync(string hangfireJobId);

    /// <summary>Получает запись истории по Id.</summary>
    Task<SyncJobHistory?> GetJobAsync(Guid historyId);

    /// <summary>Возвращает последние записи истории задач (новые первыми).</summary>
    Task<List<SyncJobHistory>> GetHistoryAsync(int limit = 20);

    /// <summary>Удаляет запись из истории. Нельзя удалить активную задачу.</summary>
    Task DeleteJobAsync(Guid historyId);

    // ── Расписания ───────────────────────────────────────────────────────────

    /// <summary>Возвращает все расписания тенанта.</summary>
    Task<List<SyncJobSchedule>> GetSchedulesAsync();

    /// <summary>Создаёт новое расписание и регистрирует его в Hangfire.</summary>
    Task<SyncJobSchedule> CreateScheduleAsync(SyncJobSchedule schedule);

    /// <summary>Обновляет параметры расписания и перерегистрирует recurring job.</summary>
    Task UpdateScheduleAsync(SyncJobSchedule schedule);

    /// <summary>Включает или отключает расписание (не удаляя запись из БД).</summary>
    Task SetScheduleEnabledAsync(Guid scheduleId, bool enabled);

    /// <summary>Удаляет расписание и отменяет recurring job в Hangfire.</summary>
    Task DeleteScheduleAsync(Guid scheduleId);
}


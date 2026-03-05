using StudioB2B.Domain.Entities.Orders;

namespace StudioB2B.Application.Common.Interfaces;

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
}


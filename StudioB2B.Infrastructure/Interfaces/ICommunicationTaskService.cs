using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Interfaces;

public interface ICommunicationTaskService
{
    Task<TaskBoardDto> GetBoardAsync(CommunicationTaskFilter filter, CancellationToken ct = default);

    /// <summary>Fast DB-only board: loads InProgress + Done + rates without calling Ozon. NewTasks will be empty.</summary>
    Task<TaskBoardDto> GetDbBoardAsync(CommunicationTaskFilter filter, CancellationToken ct = default);

    /// <summary>Fetches live New tasks from Ozon APIs, deduplicating against current InProgress in DB.</summary>
    Task<List<CommunicationTaskDto>> GetNewTasksAsync(CommunicationTaskFilter filter, CancellationToken ct = default);

    /// <summary>Streams live New tasks from Ozon one batch at a time (chats → questions → reviews).
    /// Each yielded batch can be appended to the board immediately for progressive rendering.</summary>
    IAsyncEnumerable<List<CommunicationTaskDto>> StreamNewTasksAsync(CommunicationTaskFilter filter, CancellationToken ct = default);

    /// <summary>Returns a paginated page of Done/Cancelled tasks projected directly to DTOs.</summary>
    Task<(List<CommunicationTaskDto> Items, int TotalCount)> GetDoneTasksPageAsync(
        CommunicationTaskFilter filter, int skip, int take, CancellationToken ct = default);

    Task<CommunicationTaskDetailDto?> GetTaskDetailAsync(Guid taskId, CancellationToken ct = default);

    /// <summary>Creates a DB record from a live Ozon item and immediately claims it. Returns the new task Id, or null if race-lost.</summary>
    Task<Guid?> CreateAndClaimAsync(CommunicationTaskDto liveTask, Guid userId, CancellationToken ct = default);

    /// <summary>Atomic claim: assigns task to user and starts timer. Returns false if already claimed.</summary>
    Task<bool> ClaimTaskAsync(Guid taskId, Guid userId, CancellationToken ct = default);

    /// <summary>Unclaim: deletes the DB record so the task returns to the live Ozon feed.</summary>
    Task<bool> ReleaseTaskAsync(Guid taskId, Guid userId, CancellationToken ct = default);

    /// <summary>Pause the active timer without releasing the task.</summary>
    Task<bool> PauseTaskAsync(Guid taskId, Guid userId, CancellationToken ct = default);

    /// <summary>Resume timer on a paused task.</summary>
    Task<bool> ResumeTaskAsync(Guid taskId, Guid userId, CancellationToken ct = default);

    /// <summary>Complete: stops timer, calculates payment, marks as done.</summary>
    Task<bool> CompleteTaskAsync(Guid taskId, Guid userId, CancellationToken ct = default);

    /// <summary>Возвращает выполненную задачу в «В работе» и продолжает учёт времени (новый интервал).</summary>
    Task<bool> ReopenTaskAsync(Guid taskId, Guid userId, CancellationToken ct = default);

    Task<PaymentReportDto> GetPaymentReportAsync(DateTime from, DateTime to, CancellationToken ct = default);

    /// <summary>Returns the current InProgress task for the given Ozon item (by ExternalId + client), or null if none.</summary>
    Task<CommunicationTaskDto?> FindActiveTaskAsync(string externalId, Guid marketplaceClientId, CancellationToken ct = default);

    /// <summary>Returns all currently InProgress tasks (for in-list activity indicators).</summary>
    Task<List<CommunicationTaskDto>> GetInProgressTasksAsync(CancellationToken ct = default);

    /// <summary>Returns all Done tasks completed today (UTC) for in-list done indicators.</summary>
    Task<List<CommunicationTaskDto>> GetDoneTasksTodayAsync(CancellationToken ct = default);

    /// <summary>Returns all done tasks with time entries for a specific user in the given date range.</summary>
    Task<UserTaskDetailsDto> GetUserTaskDetailsAsync(Guid userId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<PersonalStatsDto> GetPersonalStatsAsync(Guid userId, DateTime from, DateTime to, CancellationToken ct = default);

    // Payment rate CRUD
    Task<List<CommunicationPaymentRateDto>> GetPaymentRatesAsync(CancellationToken ct = default);
    Task<CommunicationPaymentRateDto> SavePaymentRateAsync(CommunicationPaymentRateDto dto, CancellationToken ct = default);
    Task<bool> DeletePaymentRateAsync(Guid id, CancellationToken ct = default);

    /// <summary>Recalculates PaymentAmount for all Done tasks using current rates. Returns count of tasks updated.</summary>
    Task<int> RecalculatePaymentsAsync(CancellationToken ct = default);

    /// <summary>Invalidates the in-memory cache for live Ozon data (chats, questions, reviews).</summary>
    void InvalidateLiveCache();
}

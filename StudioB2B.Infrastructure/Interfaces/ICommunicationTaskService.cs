using StudioB2B.Shared.DTOs;

namespace StudioB2B.Infrastructure.Interfaces;

public interface ICommunicationTaskService
{
    Task<TaskBoardDto> GetBoardAsync(CommunicationTaskFilter filter, CancellationToken ct = default);

    /// <summary>Returns a paginated page of Done/Cancelled tasks projected directly to DTOs.</summary>
    Task<(List<CommunicationTaskDto> Items, int TotalCount)> GetDoneTasksPageAsync(
        CommunicationTaskFilter filter, int skip, int take, CancellationToken ct = default);

    Task<CommunicationTaskDetailDto?> GetTaskDetailAsync(Guid taskId, CancellationToken ct = default);

    /// <summary>Atomic claim: assigns task to user and starts timer. Returns false if already claimed.</summary>
    Task<bool> ClaimTaskAsync(Guid taskId, Guid userId, CancellationToken ct = default);

    /// <summary>Unclaim: stops timer, returns task to New column.</summary>
    Task<bool> ReleaseTaskAsync(Guid taskId, Guid userId, CancellationToken ct = default);

    /// <summary>Pause the active timer without releasing the task.</summary>
    Task<bool> PauseTaskAsync(Guid taskId, Guid userId, CancellationToken ct = default);

    /// <summary>Resume timer on a paused task.</summary>
    Task<bool> ResumeTaskAsync(Guid taskId, Guid userId, CancellationToken ct = default);

    /// <summary>Complete: stops timer, calculates payment, marks as done.</summary>
    Task<bool> CompleteTaskAsync(Guid taskId, Guid userId, CancellationToken ct = default);

    Task<PaymentReportDto> GetPaymentReportAsync(DateTime from, DateTime to, CancellationToken ct = default);

    /// <summary>Returns all done tasks with time entries for a specific user in the given date range.</summary>
    Task<UserTaskDetailsDto> GetUserTaskDetailsAsync(Guid userId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<PersonalStatsDto> GetPersonalStatsAsync(Guid userId, DateTime from, DateTime to, CancellationToken ct = default);

    // Payment rate CRUD
    Task<List<CommunicationPaymentRateDto>> GetPaymentRatesAsync(CancellationToken ct = default);
    Task<CommunicationPaymentRateDto> SavePaymentRateAsync(CommunicationPaymentRateDto dto, CancellationToken ct = default);
    Task<bool> DeletePaymentRateAsync(Guid id, CancellationToken ct = default);

    /// <summary>Recalculates PaymentAmount for all Done tasks using current rates. Returns count of tasks updated.</summary>
    Task<int> RecalculatePaymentsAsync(CancellationToken ct = default);
}

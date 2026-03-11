using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using StudioB2B.Domain.Constants;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Infrastructure.MultiTenancy;

namespace StudioB2B.Infrastructure.Features.Orders;

public class OrderSyncJobService : IOrderSyncJobService
{
    private readonly ITenantDbContextCreator _dbCreator;
    private readonly ITenantProvider _tenantProvider;
    private readonly TenantHangfireManager _hangfireManager;
    private readonly ICurrentUserProvider _currentUserProvider;

    public OrderSyncJobService(
        ITenantDbContextCreator dbCreator,
        ITenantProvider tenantProvider,
        TenantHangfireManager hangfireManager,
        ICurrentUserProvider currentUserProvider)
    {
        _dbCreator = dbCreator;
        _tenantProvider = tenantProvider;
        _hangfireManager = hangfireManager;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<Guid> EnqueueSyncAsync(DateTime from, DateTime to)
    {
        EnsureTenantResolved();

        var tenantId = _tenantProvider.TenantId!.Value;
        var connectionString = _tenantProvider.ConnectionString!;

        await using var db = _dbCreator.Create();

        var history = new SyncJobHistory
        {
            JobType = SyncJobTypeEnum.Sync,
            Status = SyncJobStatusEnum.Enqueued,
            ParametersJson = JsonSerializer.Serialize(new { From = from, To = to }),
            InitiatedByUserId = _currentUserProvider.UserId,
            InitiatedByEmail = _currentUserProvider.Email
        };

        db.SyncJobHistories.Add(history);
        await db.SaveChangesAsync();

        var client = _hangfireManager.GetClient(tenantId);

        var hangfireJobId = client.Create<OrderSyncJob>(
            j => j.ExecuteSyncAsync(tenantId, connectionString, history.Id, from, to, CancellationToken.None),
            new EnqueuedState($"tenant-{tenantId:N}"));

        history.HangfireJobId = hangfireJobId;
        await db.SaveChangesAsync();

        return history.Id;
    }

    public async Task<Guid> EnqueueUpdateAsync()
    {
        EnsureTenantResolved();

        var tenantId = _tenantProvider.TenantId!.Value;
        var connectionString = _tenantProvider.ConnectionString!;

        await using var db = _dbCreator.Create();

        var history = new SyncJobHistory
        {
            JobType = SyncJobTypeEnum.Update,
            Status = SyncJobStatusEnum.Enqueued,
            InitiatedByUserId = _currentUserProvider.UserId,
            InitiatedByEmail = _currentUserProvider.Email
        };

        db.SyncJobHistories.Add(history);
        await db.SaveChangesAsync();

        var client = _hangfireManager.GetClient(tenantId);

        var hangfireJobId = client.Create<OrderSyncJob>(
            j => j.ExecuteUpdateAsync(tenantId, connectionString, history.Id, CancellationToken.None),
            new EnqueuedState($"tenant-{tenantId:N}"));

        history.HangfireJobId = hangfireJobId;
        await db.SaveChangesAsync();

        return history.Id;
    }

    public async Task<Guid> EnqueueReturnsSyncAsync(DateTime from, DateTime to)
    {
        EnsureTenantResolved();

        var tenantId         = _tenantProvider.TenantId!.Value;
        var connectionString = _tenantProvider.ConnectionString!;

        await using var db = _dbCreator.Create();

        var history = new SyncJobHistory
        {
            JobType           = SyncJobType.Returns,
            Status            = SyncJobStatus.Enqueued,
            ParametersJson    = JsonSerializer.Serialize(new { From = from, To = to }),
            InitiatedByUserId = _currentUserProvider.UserId,
            InitiatedByEmail  = _currentUserProvider.Email
        };

        db.SyncJobHistories.Add(history);
        await db.SaveChangesAsync();

        var client = _hangfireManager.GetClient(tenantId);

        var hangfireJobId = client.Create<OrderSyncJob>(
            j => j.ExecuteReturnsSyncAsync(tenantId, connectionString, history.Id, from, to, CancellationToken.None),
            new EnqueuedState($"tenant-{tenantId:N}"));

        history.HangfireJobId = hangfireJobId;
        await db.SaveChangesAsync();

        return history.Id;
    }

    public async Task CancelJobAsync(string hangfireJobId)
    {
        EnsureTenantResolved();

        var tenantId = _tenantProvider.TenantId!.Value;
        var client = _hangfireManager.GetClient(tenantId);
        client.Delete(hangfireJobId);

        await using var db = _dbCreator.Create();

        var history = await db.SyncJobHistories
            .FirstOrDefaultAsync(h => h.HangfireJobId == hangfireJobId);

        if (history is not null &&
            history.Status is SyncJobStatusEnum.Enqueued or SyncJobStatusEnum.Processing)
        {
            history.Status = SyncJobStatusEnum.Cancelled;
            history.FinishedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    public async Task<SyncJobHistory?> GetJobAsync(Guid historyId)
    {
        await using var db = _dbCreator.Create();
        return await db.SyncJobHistories
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == historyId);
    }

    public async Task<List<SyncJobHistory>> GetHistoryAsync(int limit = 20)
    {
        await using var db = _dbCreator.Create();
        return await db.SyncJobHistories
            .AsNoTracking()
            .OrderByDescending(h => h.StartedAtUtc)
            .Take(limit)
            .ToListAsync();
    }

    public async Task DeleteJobAsync(Guid historyId)
    {
        await using var db = _dbCreator.Create();

        var history = await db.SyncJobHistories.FindAsync(historyId);
        if (history is null) return;

        if (history.Status is SyncJobStatusEnum.Enqueued or SyncJobStatusEnum.Processing)
            throw new InvalidOperationException("Нельзя удалить активную задачу. Сначала остановите её.");

        db.SyncJobHistories.Remove(history);
        await db.SaveChangesAsync();
    }

    public async Task<List<SyncJobSchedule>> GetSchedulesAsync()
    {
        await using var db = _dbCreator.Create();
        return await db.SyncJobSchedules
            .AsNoTracking()
            .OrderBy(s => s.CreatedAtUtc)
            .ToListAsync();
    }

    public async Task<SyncJobSchedule> CreateScheduleAsync(SyncJobSchedule schedule)
    {
        EnsureTenantResolved();

        var tenantId = _tenantProvider.TenantId!.Value;
        var connectionString = _tenantProvider.ConnectionString!;

        schedule.Id = Guid.NewGuid();
        schedule.CreatedAtUtc = DateTime.UtcNow;
        schedule.UpdatedAtUtc = DateTime.UtcNow;
        schedule.CreatedByEmail = _currentUserProvider.Email;
        schedule.HangfireRecurringJobId = $"schedule-{schedule.Id:N}";

        await using var db = _dbCreator.Create();
        db.SyncJobSchedules.Add(schedule);
        await db.SaveChangesAsync();

        schedule.CronDescription = ScheduleCronBuilder.Describe(schedule);

        if (schedule.IsEnabled)
        {
            var manager = _hangfireManager.GetRecurringManager(tenantId);
            RegisterSchedule(manager, schedule.HangfireRecurringJobId!, tenantId, connectionString, schedule.Id, schedule.CronExpression);
        }

        return schedule;
    }

    public async Task UpdateScheduleAsync(SyncJobSchedule schedule)
    {
        EnsureTenantResolved();

        var tenantId = _tenantProvider.TenantId!.Value;
        var connectionString = _tenantProvider.ConnectionString!;

        await using var db = _dbCreator.Create();

        var existing = await db.SyncJobSchedules.FindAsync(schedule.Id)
            ?? throw new InvalidOperationException($"Schedule {schedule.Id} not found.");

        existing.JobType = schedule.JobType;
        existing.CronExpression = schedule.CronExpression;
        existing.CronDescription = ScheduleCronBuilder.Describe(schedule);
        existing.SyncParams = schedule.SyncParams;
        existing.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();

        var manager = _hangfireManager.GetRecurringManager(tenantId);
        if (existing.IsEnabled)
        {
            RegisterSchedule(manager, existing.HangfireRecurringJobId!, tenantId, connectionString, existing.Id, existing.CronExpression);
        }
    }

    public async Task SetScheduleEnabledAsync(Guid scheduleId, bool enabled)
    {
        EnsureTenantResolved();

        var tenantId = _tenantProvider.TenantId!.Value;
        var connectionString = _tenantProvider.ConnectionString!;

        await using var db = _dbCreator.Create();

        var schedule = await db.SyncJobSchedules.FindAsync(scheduleId)
            ?? throw new InvalidOperationException($"Schedule {scheduleId} not found.");

        schedule.IsEnabled = enabled;
        schedule.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var manager = _hangfireManager.GetRecurringManager(tenantId);
        if (enabled)
        {
            RegisterSchedule(manager, schedule.HangfireRecurringJobId!, tenantId, connectionString, schedule.Id, schedule.CronExpression);
        }
        else
        {
            manager.RemoveIfExists(schedule.HangfireRecurringJobId!);
        }
    }

    public async Task DeleteScheduleAsync(Guid scheduleId)
    {
        EnsureTenantResolved();

        var tenantId = _tenantProvider.TenantId!.Value;
        var manager = _hangfireManager.GetRecurringManager(tenantId);

        await using var db = _dbCreator.Create();
        var schedule = await db.SyncJobSchedules.FindAsync(scheduleId);
        if (schedule is null) return;

        if (!string.IsNullOrEmpty(schedule.HangfireRecurringJobId))
            manager.RemoveIfExists(schedule.HangfireRecurringJobId);

        db.SyncJobSchedules.Remove(schedule);
        await db.SaveChangesAsync();
    }

    private void EnsureTenantResolved()
    {
        if (!_tenantProvider.IsResolved)
            throw new InvalidOperationException(
                "Tenant is not resolved. Cannot enqueue sync job.");
    }

    /// <summary>
    /// Регистрирует recurring job в Hangfire, явно разрешая неоднозначность перегрузок AddOrUpdate.
    /// </summary>
    private static void RegisterSchedule(
        RecurringJobManager manager,
        string recurringJobId,
        Guid tenantId,
        string connectionString,
        Guid scheduleId,
        string cron)
    {
        // Используем прямой вызов через Job-объект чтобы избежать CS0121 (ambiguous overloads)
        var job = Job.FromExpression<OrderSyncJob>(
            j => j.ExecuteScheduledAsync(tenantId, connectionString, scheduleId));
        manager.AddOrUpdate(recurringJobId, job, cron, new RecurringJobOptions());
    }
}


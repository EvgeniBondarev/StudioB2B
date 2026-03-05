using Hangfire;
using Hangfire.States;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Application.Common.Interfaces;
using StudioB2B.Domain.Entities.Orders;
using StudioB2B.Infrastructure.MultiTenancy;
using StudioB2B.Infrastructure.Persistence.Tenant;

namespace StudioB2B.Infrastructure.Features.Orders;

public class OrderSyncJobService : IOrderSyncJobService
{
    private readonly TenantDbContext _db;
    private readonly ITenantProvider _tenantProvider;
    private readonly TenantHangfireManager _hangfireManager;

    public OrderSyncJobService(
        TenantDbContext db,
        ITenantProvider tenantProvider,
        TenantHangfireManager hangfireManager)
    {
        _db              = db;
        _tenantProvider  = tenantProvider;
        _hangfireManager = hangfireManager;
    }

    public async Task<Guid> EnqueueSyncAsync(DateTime from, DateTime to)
    {
        EnsureTenantResolved();

        var tenantId         = _tenantProvider.TenantId!.Value;
        var connectionString = _tenantProvider.ConnectionString!;

        var history = new SyncJobHistory
        {
            JobType  = SyncJobType.Sync,
            Status   = SyncJobStatus.Enqueued,
            DateFrom = from,
            DateTo   = to
        };

        _db.SyncJobHistories.Add(history);
        await _db.SaveChangesAsync();

        var client = _hangfireManager.GetClient(tenantId);

        var hangfireJobId = client.Create<OrderSyncJob>(
            j => j.ExecuteSyncAsync(
                tenantId, connectionString, history.Id, from, to, null!),
            new EnqueuedState($"tenant-{tenantId:N}"));

        history.HangfireJobId = hangfireJobId;
        await _db.SaveChangesAsync();

        return history.Id;
    }

    public async Task<Guid> EnqueueUpdateAsync()
    {
        EnsureTenantResolved();

        var tenantId         = _tenantProvider.TenantId!.Value;
        var connectionString = _tenantProvider.ConnectionString!;

        var history = new SyncJobHistory
        {
            JobType = SyncJobType.Update,
            Status  = SyncJobStatus.Enqueued
        };

        _db.SyncJobHistories.Add(history);
        await _db.SaveChangesAsync();

        var client = _hangfireManager.GetClient(tenantId);

        var hangfireJobId = client.Create<OrderSyncJob>(
            j => j.ExecuteUpdateAsync(
                tenantId, connectionString, history.Id, null!),
            new EnqueuedState($"tenant-{tenantId:N}"));

        history.HangfireJobId = hangfireJobId;
        await _db.SaveChangesAsync();

        return history.Id;
    }

    public async Task CancelJobAsync(string hangfireJobId)
    {
        EnsureTenantResolved();

        var tenantId = _tenantProvider.TenantId!.Value;
        var client   = _hangfireManager.GetClient(tenantId);

        // Пытаемся удалить задачу из очереди (работает для Enqueued)
        client.Delete(hangfireJobId);

        var history = await _db.SyncJobHistories
            .FirstOrDefaultAsync(h => h.HangfireJobId == hangfireJobId);

        if (history is not null &&
            history.Status is SyncJobStatus.Enqueued or SyncJobStatus.Processing)
        {
            history.Status        = SyncJobStatus.Cancelled;
            history.FinishedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public async Task<SyncJobHistory?> GetJobAsync(Guid historyId) =>
        await _db.SyncJobHistories
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == historyId);

    public async Task<List<SyncJobHistory>> GetHistoryAsync(int limit = 20) =>
        await _db.SyncJobHistories
            .AsNoTracking()
            .OrderByDescending(h => h.StartedAtUtc)
            .Take(limit)
            .ToListAsync();

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void EnsureTenantResolved()
    {
        if (!_tenantProvider.IsResolved)
            throw new InvalidOperationException(
                "Tenant is not resolved. Cannot enqueue sync job.");
    }
}


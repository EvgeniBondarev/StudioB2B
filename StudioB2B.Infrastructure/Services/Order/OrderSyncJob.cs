using System.Text.Json;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudioB2B.Domain.Constants;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Infrastructure.Persistence.Tenant;
using StudioB2B.Infrastructure.Services.Modules;
using StudioB2B.Infrastructure.Services.Ozon;

namespace StudioB2B.Infrastructure.Services.Order;

/// <summary>
/// Hangfire job-класс для фоновой синхронизации заказов.
/// Намеренно НЕ зависит от IOrderSyncService/IOrderAdapter/TenantDbContext через DI —
/// все они завязаны на scoped TenantDbContext, который требует HTTP-контекста тенанта.
/// Вместо этого весь pipeline строится вручную по переданному connectionString.
/// </summary>
public class OrderSyncJob
{
    private readonly ISyncNotificationSender _notificationSender;
    private readonly IKeyEncryptionService _encryption;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IEnumerable<IModuleActivator> _moduleActivators;

    public OrderSyncJob(ISyncNotificationSender notificationSender, IKeyEncryptionService encryption,
                        IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory,
                        IEnumerable<IModuleActivator> moduleActivators)
    {
        _notificationSender = notificationSender;
        _encryption = encryption;
        _httpClientFactory = httpClientFactory;
        _loggerFactory = loggerFactory;
        _moduleActivators = moduleActivators;
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task ExecuteSyncAsync(Guid tenantId, string connectionString, Guid historyId, DateTime from, DateTime to,
                                       CancellationToken cancellationToken = default)
    {
        await using var db = CreateDbContext(connectionString);

        var history = await db.SyncJobHistories.FirstOrDefaultAsync(h => h.Id == historyId, cancellationToken);
        if (history is null)
        {
            _loggerFactory.CreateLogger<OrderSyncJob>()
                .LogWarning("SyncJob: history record {HistoryId} not found, aborting.", historyId);
            return;
        }

        await ExecuteSyncCoreAsync(db, history, tenantId, from, to, cancellationToken);
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task ExecuteUpdateAsync(Guid tenantId, string connectionString, Guid historyId, DateTime from, DateTime to,
                                         CancellationToken cancellationToken = default)
    {
        await using var db = CreateDbContext(connectionString);

        var history = await db.SyncJobHistories.FirstOrDefaultAsync(h => h.Id == historyId, cancellationToken);
        if (history is null)
        {
            _loggerFactory.CreateLogger<OrderSyncJob>()
                .LogWarning("UpdateJob: history record {HistoryId} not found, aborting.", historyId);
            return;
        }

        await ExecuteUpdateCoreAsync(db, history, tenantId, from, to, cancellationToken);
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task ExecuteReturnsSyncAsync(Guid tenantId, string connectionString, Guid historyId, DateTime from,
                                              DateTime to, CancellationToken cancellationToken = default)
    {
        await using var db = CreateDbContext(connectionString);

        var history = await db.SyncJobHistories.FirstOrDefaultAsync(h => h.Id == historyId, cancellationToken);
        if (history is null)
        {
            _loggerFactory.CreateLogger<OrderSyncJob>()
                .LogWarning("ReturnsJob: history record {HistoryId} not found, aborting.", historyId);
            return;
        }

        await ExecuteReturnsSyncCoreAsync(db, history, tenantId, from, to, cancellationToken);
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task ExecuteScheduledAsync(Guid tenantId, string connectionString, Guid scheduleId,
                                            CancellationToken cancellationToken = default)
    {
        await using var db = CreateDbContext(connectionString);

        var schedule = await db.SyncJobSchedules.FirstOrDefaultAsync(s => s.Id == scheduleId, cancellationToken);
        if (schedule is null)
        {
            _loggerFactory.CreateLogger<OrderSyncJob>()
                .LogWarning("ScheduledJob: schedule {ScheduleId} not found, aborting.", scheduleId);
            return;
        }

        if (!schedule.IsEnabled)
        {
            _loggerFactory.CreateLogger<OrderSyncJob>()
                .LogInformation("ScheduledJob: schedule {ScheduleId} is disabled, skipping.", scheduleId);
            return;
        }

        // Читаем параметры из SyncParams JSON
        int daysBack = 7;
        if (!string.IsNullOrEmpty(schedule.SyncParams))
        {
            try
            {
                var p = JsonSerializer.Deserialize<JsonElement>(schedule.SyncParams);
                if (p.TryGetProperty("DaysBack", out var db2) && db2.TryGetInt32(out var d))
                    daysBack = d;
            }
            catch { /* используем значение по умолчанию */ }
        }

        var history = new SyncJobHistory
        {
            JobType = schedule.JobType,
            Status = SyncJobStatusEnum.Enqueued,
            ParametersJson = JsonSerializer.Serialize(new
            {
                From = DateTime.UtcNow.Date.AddDays(-daysBack),
                To = DateTime.UtcNow.Date.AddDays(1).AddTicks(-1)
            })
        };

        db.SyncJobHistories.Add(history);
        await db.SaveChangesAsync(cancellationToken);

        // Уведомляем UI сразу — чтобы запись появилась в таблице без ожидания polling
        await _notificationSender.SendJobStartedAsync(
            tenantId,
            history.Id,
            schedule.JobType == SyncJobTypeEnum.Sync ? "Sync" : schedule.JobType == SyncJobTypeEnum.Returns ? "Returns" : "Update",
            CancellationToken.None);

        if (schedule.JobType == SyncJobTypeEnum.Sync)
        {
            var from = DateTime.UtcNow.Date.AddDays(-daysBack);
            var to = DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);
            await ExecuteSyncCoreAsync(db, history, tenantId, from, to, cancellationToken);
        }
        else if (schedule.JobType == SyncJobTypeEnum.Returns)
        {
            var from = DateTime.UtcNow.Date.AddDays(-daysBack);
            var to = DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);
            await ExecuteReturnsSyncCoreAsync(db, history, tenantId, from, to, cancellationToken);
        }
        else
        {
            var from = DateTime.UtcNow.Date.AddDays(-daysBack);
            var to = DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);
            await ExecuteUpdateCoreAsync(db, history, tenantId, from, to, cancellationToken);
        }
    }

    private async Task ExecuteSyncCoreAsync(TenantDbContext db, SyncJobHistory history, Guid tenantId, DateTime from,
                                            DateTime to, CancellationToken cancellationToken)
    {
        history.Status = SyncJobStatusEnum.Processing;
        await db.SaveChangesAsync(CancellationToken.None);

        var logger = _loggerFactory.CreateLogger<OrderSyncJob>();
        try
        {
            logger.LogInformation(
                "SyncJob: starting sync for tenant {TenantId}, period {From}–{To}.",
                tenantId, from, to);

            var allowedClientIds = await GetAllowedClientIdsAsync(db, history.InitiatedByUserId, cancellationToken);
            var syncService = BuildSyncService(db);
            var summary = await syncService.SyncAllAsync(from, to, cancellationToken, async msg =>
            {
                await _notificationSender.SendJobProgressAsync(tenantId, history.Id, msg, CancellationToken.None);
            }, allowedClientIds);

            history.Status = SyncJobStatusEnum.Succeeded;
            history.FinishedAtUtc = DateTime.UtcNow;
            history.ResultJson = JsonSerializer.Serialize(summary);

            logger.LogInformation("SyncJob: sync completed for tenant {TenantId}.", tenantId);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("SyncJob: sync cancelled for tenant {TenantId}.", tenantId);
            history.Status = SyncJobStatusEnum.Cancelled;
            history.FinishedAtUtc = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SyncJob: sync failed for tenant {TenantId}.", tenantId);
            history.Status = SyncJobStatusEnum.Failed;
            history.FinishedAtUtc = DateTime.UtcNow;
            history.ErrorMessage = ex.Message;
        }
        finally
        {
            await db.SaveChangesAsync(CancellationToken.None);
            await _notificationSender.SendJobCompletedAsync(tenantId, history.Id, history.Status.ToString(), "Sync", CancellationToken.None);
        }
    }

    private async Task ExecuteReturnsSyncCoreAsync(TenantDbContext db, SyncJobHistory history, Guid tenantId, DateTime from,
                                                   DateTime to, CancellationToken cancellationToken)
    {
        history.Status = SyncJobStatusEnum.Processing;
        await db.SaveChangesAsync(CancellationToken.None);

        var logger = _loggerFactory.CreateLogger<OrderSyncJob>();
        try
        {
            logger.LogInformation("ReturnsJob: starting returns sync for tenant {TenantId}, period {From}–{To}.",
                                  tenantId, from, to);

            var allowedClientIds = await GetAllowedClientIdsAsync(db, history.InitiatedByUserId, cancellationToken);
            var syncService = BuildReturnsSyncService(db);
            var summary = await syncService.SyncAllAsync(from, to, cancellationToken, allowedClientIds);

            history.Status = SyncJobStatusEnum.Succeeded;
            history.FinishedAtUtc = DateTime.UtcNow;
            history.ResultJson = JsonSerializer.Serialize(summary);

            logger.LogInformation("ReturnsJob: completed for tenant {TenantId}.", tenantId);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("ReturnsJob: cancelled for tenant {TenantId}.", tenantId);
            history.Status = SyncJobStatusEnum.Cancelled;
            history.FinishedAtUtc = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ReturnsJob: failed for tenant {TenantId}.", tenantId);
            history.Status = SyncJobStatusEnum.Failed;
            history.FinishedAtUtc = DateTime.UtcNow;
            history.ErrorMessage = ex.Message;
        }
        finally
        {
            await db.SaveChangesAsync(CancellationToken.None);
            await _notificationSender.SendJobCompletedAsync(tenantId, history.Id, history.Status.ToString(), "Returns", CancellationToken.None);
        }
    }

    private async Task ExecuteUpdateCoreAsync(TenantDbContext db, SyncJobHistory history, Guid tenantId, DateTime from,
                                              DateTime to, CancellationToken cancellationToken)
    {
        history.Status = SyncJobStatusEnum.Processing;
        await db.SaveChangesAsync(CancellationToken.None);

        var logger = _loggerFactory.CreateLogger<OrderSyncJob>();
        try
        {
            logger.LogInformation(
                "UpdateJob: starting status update for tenant {TenantId}, period {From}\u2013{To}.",
                tenantId, from, to);

            var allowedClientIds = await GetAllowedClientIdsAsync(db, history.InitiatedByUserId, cancellationToken);
            var syncService = BuildSyncService(db);
            var summary = await syncService.UpdateAllAsync(from, to, cancellationToken, async msg =>
            {
                await _notificationSender.SendJobProgressAsync(tenantId, history.Id, msg, CancellationToken.None);
            }, allowedClientIds);

            history.Status = SyncJobStatusEnum.Succeeded;
            history.FinishedAtUtc = DateTime.UtcNow;
            history.ResultJson = JsonSerializer.Serialize(summary);

            logger.LogInformation("UpdateJob: completed for tenant {TenantId}.", tenantId);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("UpdateJob: cancelled for tenant {TenantId}.", tenantId);
            history.Status = SyncJobStatusEnum.Cancelled;
            history.FinishedAtUtc = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "UpdateJob: failed for tenant {TenantId}.", tenantId);
            history.Status = SyncJobStatusEnum.Failed;
            history.FinishedAtUtc = DateTime.UtcNow;
            history.ErrorMessage = ex.Message;
        }
        finally
        {
            await db.SaveChangesAsync(CancellationToken.None);
            await _notificationSender.SendJobCompletedAsync(tenantId, history.Id, history.Status.ToString(), "Update", CancellationToken.None);
        }
    }

    /// <summary>
    /// Строит OzonReturnsSyncService с явным TenantDbContext и OzonApiClient.
    /// </summary>
    private OzonReturnsSyncService BuildReturnsSyncService(TenantDbContext db)
    {
        var apiClient = new OzonApiClient(
            _httpClientFactory,
            _encryption,
            _loggerFactory.CreateLogger<OzonApiClient>());

        return new OzonReturnsSyncService(
            db,
            apiClient,
            _loggerFactory.CreateLogger<OzonReturnsSyncService>());
    }

    /// <summary>
    /// Строит OrderSyncService с явным TenantDbContext и OzonFbsOrderAdapter.
    /// </summary>
    private OrderSyncService BuildSyncService(TenantDbContext db)
    {
        var apiClient = new OzonApiClient(
            _httpClientFactory,
            _encryption,
            _loggerFactory.CreateLogger<OzonApiClient>());

        var moduleService = new ModuleService(
            db,
            _moduleActivators,
            _loggerFactory.CreateLogger<ModuleService>());

        var fbsAdapter = new OzonFbsOrderAdapter(
            apiClient,
            db,
            _loggerFactory.CreateLogger<OzonFbsOrderAdapter>(),
            moduleService);

        var fboAdapter = new OzonFboOrderAdapter(
            apiClient,
            db,
            _loggerFactory.CreateLogger<OzonFboOrderAdapter>(),
            _loggerFactory.CreateLogger<OzonFbsOrderAdapter>(),
            moduleService);

        return new OrderSyncService(
            db,
            new IOrderAdapter[] { fbsAdapter, fboAdapter },
            _loggerFactory.CreateLogger<OrderSyncService>());
    }

    private static TenantDbContext CreateDbContext(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
        optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        return new TenantDbContext(optionsBuilder.Options, currentUserProvider: null);
    }

    /// <summary>
    /// Resolves the set of allowed MarketplaceClient IDs for the given user based on their permissions.
    /// Returns <c>null</c> when there are no restrictions (full access or system job).
    /// </summary>
    private static async Task<HashSet<Guid>?> GetAllowedClientIdsAsync(
        TenantDbContext db, Guid? userId, CancellationToken ct)
    {
        if (userId is null)
            return null; // System/scheduled job — no restrictions

        var permissionIds = await db.UserPermissions
            .AsNoTracking()
            .Where(up => up.UserId == userId)
            .Select(up => up.PermissionId)
            .ToListAsync(ct);

        if (permissionIds.Count == 0)
            return null; // No permissions assigned → no restrictions

        // Full-access permission → no client restrictions
        var hasFullAccess = await db.Permissions
            .AsNoTracking()
            .AnyAsync(p => permissionIds.Contains(p.Id) && p.IsFullAccess, ct);

        if (hasFullAccess)
            return null;

        var allowedIds = await db.BlockedEntities
            .AsNoTracking()
            .Where(be => permissionIds.Contains(be.PermissionId)
                         && be.EntityType == BlockedEntityTypeEnum.MarketplaceClient)
            .Select(be => be.EntityId)
            .ToHashSetAsync(ct);

        // Empty whitelist = no MarketplaceClient restriction configured → unrestricted
        return allowedIds.Count > 0 ? allowedIds : null;
    }
}

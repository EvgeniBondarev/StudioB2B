using System.Text.Json;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudioB2B.Application.Common.Interfaces;
using StudioB2B.Domain.Entities.Orders;
using StudioB2B.Infrastructure.Integrations.Ozon;
using StudioB2B.Infrastructure.Persistence.Tenant;
using StudioB2B.Infrastructure.Services;

namespace StudioB2B.Infrastructure.Features.Orders;

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

    public OrderSyncJob(
        ISyncNotificationSender notificationSender,
        IKeyEncryptionService encryption,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory)
    {
        _notificationSender = notificationSender;
        _encryption         = encryption;
        _httpClientFactory  = httpClientFactory;
        _loggerFactory      = loggerFactory;
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task ExecuteSyncAsync(
        Guid tenantId,
        string connectionString,
        Guid historyId,
        DateTime from,
        DateTime to,
        IJobCancellationToken jobToken)
    {
        await using var db = CreateDbContext(connectionString);

        var history = await db.SyncJobHistories.FirstOrDefaultAsync(h => h.Id == historyId);
        if (history is null)
        {
            _loggerFactory.CreateLogger<OrderSyncJob>()
                .LogWarning("SyncJob: history record {HistoryId} not found, aborting.", historyId);
            return;
        }

        history.Status = SyncJobStatus.Processing;
        await db.SaveChangesAsync();

        var logger = _loggerFactory.CreateLogger<OrderSyncJob>();
        try
        {
            logger.LogInformation(
                "SyncJob: starting sync for tenant {TenantId}, period {From}–{To}.",
                tenantId, from, to);

            var syncService = BuildSyncService(db);
            var summary     = await syncService.SyncAllAsync(from, to, jobToken.ShutdownToken);

            history.Status        = SyncJobStatus.Succeeded;
            history.FinishedAtUtc = DateTime.UtcNow;
            history.ResultJson    = JsonSerializer.Serialize(summary);

            logger.LogInformation("SyncJob: sync completed for tenant {TenantId}.", tenantId);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("SyncJob: sync cancelled for tenant {TenantId}.", tenantId);
            history.Status        = SyncJobStatus.Cancelled;
            history.FinishedAtUtc = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SyncJob: sync failed for tenant {TenantId}.", tenantId);
            history.Status        = SyncJobStatus.Failed;
            history.FinishedAtUtc = DateTime.UtcNow;
            history.ErrorMessage  = ex.Message;
        }
        finally
        {
            await db.SaveChangesAsync();
            await _notificationSender.SendJobCompletedAsync(
                tenantId, historyId, history.Status.ToString(), "Sync");
        }
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task ExecuteUpdateAsync(
        Guid tenantId,
        string connectionString,
        Guid historyId,
        IJobCancellationToken jobToken)
    {
        await using var db = CreateDbContext(connectionString);

        var history = await db.SyncJobHistories.FirstOrDefaultAsync(h => h.Id == historyId);
        if (history is null)
        {
            _loggerFactory.CreateLogger<OrderSyncJob>()
                .LogWarning("UpdateJob: history record {HistoryId} not found, aborting.", historyId);
            return;
        }

        history.Status = SyncJobStatus.Processing;
        await db.SaveChangesAsync();

        var logger = _loggerFactory.CreateLogger<OrderSyncJob>();
        try
        {
            logger.LogInformation(
                "UpdateJob: starting status update for tenant {TenantId}.", tenantId);

            var syncService = BuildSyncService(db);
            var summary     = await syncService.UpdateAllAsync(jobToken.ShutdownToken);

            history.Status        = SyncJobStatus.Succeeded;
            history.FinishedAtUtc = DateTime.UtcNow;
            history.ResultJson    = JsonSerializer.Serialize(summary);

            logger.LogInformation("UpdateJob: completed for tenant {TenantId}.", tenantId);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("UpdateJob: cancelled for tenant {TenantId}.", tenantId);
            history.Status        = SyncJobStatus.Cancelled;
            history.FinishedAtUtc = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "UpdateJob: failed for tenant {TenantId}.", tenantId);
            history.Status        = SyncJobStatus.Failed;
            history.FinishedAtUtc = DateTime.UtcNow;
            history.ErrorMessage  = ex.Message;
        }
        finally
        {
            await db.SaveChangesAsync();
            await _notificationSender.SendJobCompletedAsync(
                tenantId, historyId, history.Status.ToString(), "Update");
        }
    }

    // ── Pipeline factory ─────────────────────────────────────────────────────

    /// <summary>
    /// Строит OrderSyncService с явным TenantDbContext и OzonFbsOrderAdapter.
    /// Ни один из них не проходит через scoped DI — нет зависимости от HTTP-контекста.
    /// </summary>
    private OrderSyncService BuildSyncService(TenantDbContext db)
    {
        var apiClient = new OzonApiClient(
            _httpClientFactory,
            _encryption,
            _loggerFactory.CreateLogger<OzonApiClient>());

        var adapter = new OzonFbsOrderAdapter(
            apiClient,
            db,
            _loggerFactory.CreateLogger<OzonFbsOrderAdapter>());

        return new OrderSyncService(
            db,
            adapter,
            _loggerFactory.CreateLogger<OrderSyncService>());
    }

    private static TenantDbContext CreateDbContext(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
        optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        return new TenantDbContext(optionsBuilder.Options, currentUserProvider: null);
    }
}

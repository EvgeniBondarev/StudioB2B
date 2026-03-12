using System.Collections.Concurrent;
using Hangfire;
using Hangfire.AspNetCore;
using Hangfire.MySql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StudioB2B.Infrastructure.Features.Orders;
using StudioB2B.Infrastructure.Persistence.Master;
using StudioB2B.Infrastructure.Persistence.Tenant;

namespace StudioB2B.Infrastructure.MultiTenancy;

/// <summary>
/// IHostedService + Singleton.
/// При старте поднимает по одному Hangfire BackgroundJobServer на каждого активного тенанта.
/// Хранит ConcurrentDictionary&lt;tenantId, TenantHangfireContext&gt; для изоляции очередей.
/// </summary>
public sealed class TenantHangfireManager : IHostedService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TenantHangfireManager> _logger;
    private readonly ConcurrentDictionary<Guid, TenantHangfireContext> _tenants = new();

    public TenantHangfireManager(IServiceScopeFactory scopeFactory, ILogger<TenantHangfireManager> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("TenantHangfireManager: starting per-tenant Hangfire servers...");

        await using var scope = _scopeFactory.CreateAsyncScope();
        var masterDb = scope.ServiceProvider.GetRequiredService<MasterDbContext>();

        var tenants = await masterDb.Tenants
            .AsNoTracking()
            .Where(t => t.IsActive && !t.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var tenant in tenants)
        {
            await CleanupStaleRecurringJobsAsync(tenant.Id, tenant.ConnectionString, cancellationToken);

            CreateAndRegisterServer(tenant.Id, tenant.ConnectionString, tenant.Subdomain);
            await RestoreSchedulesAsync(tenant.Id, tenant.ConnectionString, cancellationToken);
        }

        _logger.LogInformation(
            "TenantHangfireManager: {Count} tenant server(s) started.", _tenants.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("TenantHangfireManager: stopping all tenant Hangfire servers...");
        Dispose();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Добавляет сервер нового тенанта в рантайме (вызывается из TenantService.RegisterAsync).
    /// Идемпотентен — повторный вызов для того же тенанта игнорируется.
    /// </summary>
    public Task AddTenant(Guid tenantId, string connectionString, CancellationToken ct = default)
    {
        if (_tenants.ContainsKey(tenantId))
        {
            _logger.LogDebug(
                "TenantHangfireManager: server for tenant {TenantId} already registered, skipping.",
                tenantId);
            return Task.CompletedTask;
        }

        CreateAndRegisterServer(tenantId, connectionString, subdomain: tenantId.ToString("N"));
        _logger.LogInformation(
            "TenantHangfireManager: server for new tenant {TenantId} registered.",
            tenantId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Возвращает IBackgroundJobClient для указанного тенанта.
    /// </summary>
    /// <exception cref="InvalidOperationException">Тенант не найден.</exception>
    public IBackgroundJobClient GetClient(Guid tenantId)
    {
        if (_tenants.TryGetValue(tenantId, out var ctx))
            return ctx.Client;

        throw new InvalidOperationException(
            $"Hangfire client for tenant {tenantId} is not registered. " +
            "The tenant may be inactive or not yet initialized.");
    }

    /// <summary>
    /// Возвращает RecurringJobManager для указанного тенанта.
    /// </summary>
    public RecurringJobManager GetRecurringManager(Guid tenantId)
    {
        if (_tenants.TryGetValue(tenantId, out var ctx))
            return ctx.RecurringJobManager;

        throw new InvalidOperationException(
            $"Hangfire recurring manager for tenant {tenantId} is not registered.");
    }

    // ── Private ──────────────────────────────────────────────────────────────

    private void CreateAndRegisterServer(Guid tenantId, string connectionString, string subdomain)
    {
        try
        {
            var storageOptions = new MySqlStorageOptions
            {
                TablesPrefix = "Hangfire_",
                PrepareSchemaIfNecessary = true
            };

            var storage = new MySqlStorage(connectionString, storageOptions);

            var serverOptions = new BackgroundJobServerOptions
            {
                ServerName = $"studiob2b-{subdomain}",
                Queues = [$"tenant-{tenantId:N}", "default"],
                Activator = new AspNetCoreJobActivator(_scopeFactory)
            };

            var server = new BackgroundJobServer(serverOptions, storage);
            var client = new BackgroundJobClient(storage);
            var recurringManager = new RecurringJobManager(storage);

            var ctx = new TenantHangfireContext(client, server, storage, recurringManager);

            if (!_tenants.TryAdd(tenantId, ctx))
            {
                ctx.Dispose();
                _logger.LogDebug(
                    "TenantHangfireManager: race condition resolved for tenant {TenantId}.",
                    tenantId);
            }
            else
            {
                _logger.LogDebug(
                    "TenantHangfireManager: server registered for tenant {TenantId} ({Subdomain}).",
                    tenantId, subdomain);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "TenantHangfireManager: failed to start server for tenant {TenantId} ({Subdomain}).",
                tenantId, subdomain);
        }
    }

    /// <summary>
    /// Удаляет из Hangfire-хранилища все recurring jobs тенанта, связанные с расписаниями,
    /// а также все enqueued/failed/scheduled jobs с устаревшими сигнатурами методов
    /// (например, после рефакторинга IJobCancellationToken → CancellationToken).
    /// </summary>
    private async Task CleanupStaleRecurringJobsAsync(Guid tenantId, string connectionString, CancellationToken ct)
    {
        try
        {
            var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            await using var db = new TenantDbContext(optionsBuilder.Options);

            // ── 1. Удаляем stale enqueued/scheduled/failed jobs со старой сигнатурой ──
            // Hangfire сериализует тип параметров в InvocationData как JSON-строку.
            // Ищем любые записи где упоминается IJobCancellationToken.
            var deleted = await db.Database.ExecuteSqlRawAsync(
                """
                DELETE FROM Hangfire_Job
                WHERE InvocationData LIKE '%IJobCancellationToken%'
                   OR Arguments       LIKE '%IJobCancellationToken%'
                """, ct);

            if (deleted > 0)
                _logger.LogInformation(
                    "TenantHangfireManager: deleted {Count} stale job(s) with old signatures for tenant {TenantId}.",
                    deleted, tenantId);

            // ── 2. Удаляем stale recurring jobs ──────────────────────────────────
            var jobIds = await db.SyncJobSchedules
                .Where(s => s.HangfireRecurringJobId != null)
                .Select(s => s.HangfireRecurringJobId!)
                .ToListAsync(ct);

            if (jobIds.Count > 0)
            {
                var storageOptions = new MySqlStorageOptions
                {
                    TablesPrefix             = "Hangfire_",
                    PrepareSchemaIfNecessary = false
                };
                using var storage = new MySqlStorage(connectionString, storageOptions);
                var manager = new RecurringJobManager(storage);
                foreach (var jobId in jobIds)
                    manager.RemoveIfExists(jobId);

                _logger.LogInformation(
                    "TenantHangfireManager: removed {Count} stale recurring job(s) for tenant {TenantId}.",
                    jobIds.Count, tenantId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "TenantHangfireManager: failed to cleanup stale jobs for tenant {TenantId}.", tenantId);
        }
    }

    /// <summary>
    /// Восстанавливает активные расписания из БД тенанта после рестарта приложения.
    /// </summary>
    private async Task RestoreSchedulesAsync(Guid tenantId, string connectionString, CancellationToken ct = default)
    {
        try
        {
            var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            await using var db = new TenantDbContext(optionsBuilder.Options);

            var schedules = await db.SyncJobSchedules
                .Where(s => s.IsEnabled && s.HangfireRecurringJobId != null)
                .ToListAsync(ct);

            if (schedules.Count == 0) return;

            if (!_tenants.TryGetValue(tenantId, out var ctx)) return;

            foreach (var schedule in schedules)
            {
                var job = Hangfire.Common.Job.FromExpression<OrderSyncJob>(
                    j => j.ExecuteScheduledAsync(tenantId, connectionString, schedule.Id, ct));
                ctx.RecurringJobManager.AddOrUpdate(
                    schedule.HangfireRecurringJobId!,
                    job,
                    schedule.CronExpression,
                    new RecurringJobOptions());
            }

            _logger.LogInformation(
                "TenantHangfireManager: restored {Count} schedule(s) for tenant {TenantId}.",
                schedules.Count, tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "TenantHangfireManager: failed to restore schedules for tenant {TenantId}.", tenantId);
        }
    }

    public void Dispose()
    {
        foreach (var ctx in _tenants.Values)
        {
            try { ctx.Dispose(); }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "TenantHangfireManager: error disposing tenant Hangfire context.");
            }
        }

        _tenants.Clear();
    }
}


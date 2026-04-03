using Hangfire;
using Hangfire.AspNetCore;
using Hangfire.MySql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StudioB2B.Infrastructure.Persistence.Master;

namespace StudioB2B.Infrastructure.Services.MultiTenancy;

/// <summary>
/// Singleton IHostedService.
/// Поднимает один BackgroundJobServer и RecurringJobManager поверх мастер-БД.
/// Используется для master-level задач, например бэкапов тенант-БД.
/// </summary>
public sealed class MasterHangfireManager : IHostedService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MasterHangfireManager> _logger;
    private readonly string _connectionString;

    private BackgroundJobServer? _server;
    private MySqlStorage? _storage;
    private IBackgroundJobClient? _client;
    private RecurringJobManager? _recurringJobManager;

    public IBackgroundJobClient Client =>
        _client ?? throw new InvalidOperationException("MasterHangfireManager is not started.");

    public RecurringJobManager RecurringJobManager =>
        _recurringJobManager ?? throw new InvalidOperationException("MasterHangfireManager is not started.");

    public MasterHangfireManager(
        IServiceScopeFactory scopeFactory,
        ILogger<MasterHangfireManager> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _connectionString = configuration.GetConnectionString("MasterDb")
            ?? throw new InvalidOperationException("MasterDb connection string is not configured.");
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MasterHangfireManager: starting master Hangfire server...");

        try
        {
            var storageOptions = new MySqlStorageOptions
            {
                TablesPrefix = "MasterHangfire_",
                PrepareSchemaIfNecessary = true
            };

            _storage = new MySqlStorage(_connectionString, storageOptions);

            var serverOptions = new BackgroundJobServerOptions
            {
                ServerName = "studiob2b-master",
                Queues = ["master-backup", "default"],
                Activator = new AspNetCoreJobActivator(_scopeFactory)
            };

            _server = new BackgroundJobServer(serverOptions, _storage);
            _client = new BackgroundJobClient(_storage);
            _recurringJobManager = new RecurringJobManager(_storage);

            _logger.LogInformation("MasterHangfireManager: master Hangfire server started.");

            await RestoreBackupSchedulesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MasterHangfireManager: failed to start master Hangfire server.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MasterHangfireManager: stopping master Hangfire server...");
        Dispose();
        return Task.CompletedTask;
    }

    private async Task RestoreBackupSchedulesAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var masterDb = scope.ServiceProvider.GetRequiredService<MasterDbContext>();

            var schedules = await masterDb.TenantBackupSchedules
                .Include(s => s.Tenant)
                .Where(s => s.IsEnabled && s.HangfireJobId != null)
                .ToListAsync(ct);

            if (schedules.Count == 0) return;

            foreach (var schedule in schedules)
            {
                var tenantId = schedule.TenantId;
                var connectionString = schedule.Tenant!.ConnectionString;
                var subdomain = schedule.Tenant.Subdomain;

                var job = Hangfire.Common.Job.FromExpression<TenantBackupJob>(
                    j => j.ExecuteAsync(tenantId, connectionString, subdomain, CancellationToken.None));

                _recurringJobManager!.AddOrUpdate(schedule.HangfireJobId!, job,
                    schedule.CronExpression, new RecurringJobOptions());
            }

            _logger.LogInformation("MasterHangfireManager: restored {Count} backup schedule(s).", schedules.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MasterHangfireManager: failed to restore backup schedules.");
        }
    }

    public void Dispose()
    {
        try { _server?.Dispose(); } catch { /* ignore */ }
        try { _storage?.Dispose(); } catch { /* ignore */ }
    }
}


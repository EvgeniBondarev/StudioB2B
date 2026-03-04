using System.Collections.Concurrent;
using Hangfire;
using Hangfire.AspNetCore;
using Hangfire.MySql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StudioB2B.Infrastructure.Persistence.Master;

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

    public TenantHangfireManager(
        IServiceScopeFactory scopeFactory,
        ILogger<TenantHangfireManager> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    // ── IHostedService ───────────────────────────────────────────────────────

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
            CreateAndRegisterServer(tenant.Id, tenant.ConnectionString, tenant.Subdomain);

        _logger.LogInformation(
            "TenantHangfireManager: {Count} tenant server(s) started.", _tenants.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("TenantHangfireManager: stopping all tenant Hangfire servers...");
        Dispose();
        return Task.CompletedTask;
    }

    // ── Public API ───────────────────────────────────────────────────────────

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

    // ── Private ──────────────────────────────────────────────────────────────

    private void CreateAndRegisterServer(Guid tenantId, string connectionString, string subdomain)
    {
        try
        {
            var storageOptions = new MySqlStorageOptions
            {
                TablesPrefix             = "Hangfire_",
                PrepareSchemaIfNecessary = true
            };

            var storage = new MySqlStorage(connectionString, storageOptions);

            var serverOptions = new BackgroundJobServerOptions
            {
                ServerName  = $"studiob2b-{subdomain}",
                Queues      = new[] { $"tenant-{tenantId:N}" },
                // AspNetCoreJobActivator создаёт DI-scope для каждой задачи —
                // единственный способ получить зависимости без parameterless constructor
                Activator   = new AspNetCoreJobActivator(_scopeFactory)
            };

            var server = new BackgroundJobServer(serverOptions, storage);
            var client = new BackgroundJobClient(storage);

            var ctx = new TenantHangfireContext(client, server, storage);

            if (!_tenants.TryAdd(tenantId, ctx))
            {
                // Race condition — другой поток уже добавил этот тенант
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

    // ── IDisposable ──────────────────────────────────────────────────────────

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


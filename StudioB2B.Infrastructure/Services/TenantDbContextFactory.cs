using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Infrastructure.Persistence.Tenant;

namespace StudioB2B.Infrastructure.Services;

public class TenantDbContextFactory : ITenantDbContextFactory
{
    // Cache ServerVersion per connection string — avoids a synchronous DB ping on every CreateDbContext() call.
    private static readonly ConcurrentDictionary<string, ServerVersion> _serverVersionCache = new();

    // Track tenants whose migrations have already been checked — avoids hitting the DB on every CreateDbContext().
    private static readonly ConcurrentDictionary<string, bool> _migrationsChecked = new();

    private readonly ITenantProvider _tenantProvider;
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly ILogger<TenantDbContextFactory> _logger;

    public TenantDbContextFactory(
        ITenantProvider tenantProvider,
        ICurrentUserProvider currentUserProvider,
        ILogger<TenantDbContextFactory> logger)
    {
        _tenantProvider = tenantProvider;
        _currentUserProvider = currentUserProvider;
        _logger = logger;
    }

    public TenantDbContext CreateDbContext()
    {
        if (!_tenantProvider.IsResolved)
        {
            throw new InvalidOperationException("Tenant is not resolved. Cannot create TenantDbContext.");
        }

        var connectionString = _tenantProvider.ConnectionString!;

        var serverVersion = _serverVersionCache.GetOrAdd(connectionString, cs => ServerVersion.AutoDetect(cs));

        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
        optionsBuilder.UseMySql(connectionString, serverVersion);

        _logger.LogDebug("Creating TenantDbContext for tenant {TenantId}", _tenantProvider.TenantId);

        var context = new TenantDbContext(optionsBuilder.Options, _currentUserProvider);

        // Check & apply pending migrations only once per tenant per app lifetime.
        var tenantId = _tenantProvider.TenantId.ToString();
        if (tenantId != null && !_migrationsChecked.ContainsKey(tenantId))
        {
            try
            {
                var pending = context.Database.GetPendingMigrations().ToList();
                if (pending.Count > 0)
                {
                    _logger.LogInformation(
                        "Applying {Count} pending tenant migrations for {TenantId}: {Migrations}",
                        pending.Count,
                        _tenantProvider.TenantId,
                        string.Join(", ", pending));

                    context.Database.Migrate();

                    _logger.LogInformation(
                        "Tenant migrations applied successfully for {TenantId}",
                        _tenantProvider.TenantId);
                }

                _migrationsChecked.TryAdd(tenantId, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to apply tenant migrations for {TenantId}",
                    _tenantProvider.TenantId);
                throw;
            }
        }

        return context;
    }
}

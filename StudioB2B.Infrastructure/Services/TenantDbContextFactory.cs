using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Infrastructure.Persistence.Tenant;

namespace StudioB2B.Infrastructure.Services;

public class TenantDbContextFactory : ITenantDbContextFactory
{
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

        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();

        var connectionString = _tenantProvider.ConnectionString!;
        optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

        _logger.LogDebug("Creating TenantDbContext for tenant {TenantId}", _tenantProvider.TenantId);

        var context = new TenantDbContext(optionsBuilder.Options, _currentUserProvider);

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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to apply tenant migrations for {TenantId}",
                _tenantProvider.TenantId);
            throw;
        }

        return context;
    }
}

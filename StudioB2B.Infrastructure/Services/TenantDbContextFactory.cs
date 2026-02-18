using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudioB2B.Application.Common.Interfaces;
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

        // MySQL с автоопределением версии
        var connectionString = _tenantProvider.ConnectionString!;
        optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Creating TenantDbContext for tenant {TenantId}", _tenantProvider.TenantId);
        }

        return new TenantDbContext(optionsBuilder.Options, _currentUserProvider.UserId);
    }
}

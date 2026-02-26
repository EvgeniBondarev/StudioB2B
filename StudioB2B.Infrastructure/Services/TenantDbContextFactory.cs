using Microsoft.EntityFrameworkCore;
using StudioB2B.Application.Common.Interfaces;
using StudioB2B.Infrastructure.Persistence.Tenant;

namespace StudioB2B.Infrastructure.Services;

public class TenantDbContextFactory : ITenantDbContextFactory
{
    private readonly ITenantProvider _tenantProvider;

    public TenantDbContextFactory(ITenantProvider tenantProvider)
    {
        _tenantProvider = tenantProvider;
    }

    public TenantDbContext CreateDbContext()
    {
        if (!_tenantProvider.IsResolved)
            throw new InvalidOperationException("Tenant is not resolved. Cannot create TenantDbContext.");

        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
        var connectionString = _tenantProvider.ConnectionString!;
        optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

        return new TenantDbContext(optionsBuilder.Options);
    }
}

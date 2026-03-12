using Microsoft.EntityFrameworkCore;
using StudioB2B.Infrastructure.Features.Orders;
using StudioB2B.Infrastructure.Persistence.Tenant;
using ICurrentUserProvider = StudioB2B.Infrastructure.Interfaces.ICurrentUserProvider;
using ITenantProvider = StudioB2B.Infrastructure.Interfaces.ITenantProvider;

namespace StudioB2B.Infrastructure.MultiTenancy.Services;

public class TenantDbContextCreator : ITenantDbContextCreator
{
    private readonly ITenantProvider _tenantProvider;
    private readonly ICurrentUserProvider? _currentUserProvider;

    public TenantDbContextCreator(
        ITenantProvider tenantProvider,
        ICurrentUserProvider? currentUserProvider = null)
    {
        _tenantProvider = tenantProvider;
        _currentUserProvider = currentUserProvider;
    }

    public TenantDbContext Create()
    {
        if (!_tenantProvider.IsResolved || string.IsNullOrEmpty(_tenantProvider.ConnectionString))
            throw new InvalidOperationException("Tenant is not resolved. Cannot create TenantDbContext.");

        var cs = _tenantProvider.ConnectionString;
        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
        optionsBuilder.UseMySql(cs, ServerVersion.AutoDetect(cs));

        return new TenantDbContext(optionsBuilder.Options, _currentUserProvider);
    }
}


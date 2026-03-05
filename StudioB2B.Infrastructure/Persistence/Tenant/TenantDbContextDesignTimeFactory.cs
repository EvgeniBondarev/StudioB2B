using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace StudioB2B.Infrastructure.Persistence.Tenant;

/// <summary>
/// Used by EF Core tools (migrations) at design time.
/// Provides a TenantDbContext with a stub connection string so that
/// the real tenant-resolution logic (TenantMiddleware / TenantProvider) is never invoked.
/// </summary>
public class TenantDbContextDesignTimeFactory : IDesignTimeDbContextFactory<TenantDbContext>
{
    public TenantDbContext CreateDbContext(string[] args)
    {
        // Design-time stub connection string – the actual database does not need to exist.
        const string designTimeConnectionString =
            "Server=localhost;Port=3345;Database=StudioB2B_Tenant_DesignTime;User=root;Password=root;Allow User Variables=true;AllowPublicKeyRetrieval=True;";

        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
        optionsBuilder.UseMySql(
            designTimeConnectionString,
            ServerVersion.AutoDetect(designTimeConnectionString));

        return new TenantDbContext(optionsBuilder.Options, currentUserProvider: null);
    }
}


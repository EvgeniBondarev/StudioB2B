using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace StudioB2B.Infrastructure.Persistence.Tenant;

/// <summary>
/// Design-time factory для TenantDbContext (используется для создания миграций)
/// </summary>
public class DesignTimeTenantDbContextFactory : IDesignTimeDbContextFactory<TenantDbContext>
{
    public TenantDbContext CreateDbContext(string[] args)
    {
        // Временный connection string для создания миграций
        // Эта база не создаётся, используется только для генерации SQL
        var connectionString = "Server=localhost;Database=StudioB2B_Tenant_Design;User=root;Password=root;";

        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
        optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

        return new TenantDbContext(optionsBuilder.Options);
    }
}

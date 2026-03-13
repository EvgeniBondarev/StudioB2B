using StudioB2B.Infrastructure.Persistence.Tenant;

namespace StudioB2B.Infrastructure.Interfaces;

/// <summary>
/// Фабрика для создания TenantDbContext с динамическим connection string
/// </summary>
public interface ITenantDbContextFactory
{
    TenantDbContext CreateDbContext();
}

using Microsoft.EntityFrameworkCore;
using StudioB2B.Domain.Entities.Tenants;
using TenantEntity = StudioB2B.Domain.Entities.Tenants.Tenant;

namespace StudioB2B.Infrastructure.Persistence.Master;

/// <summary>
/// Master Database Context - хранит данные о тенантах, тарифах, глобальные настройки
/// </summary>
public class MasterDbContext : DbContext
{
    public MasterDbContext(DbContextOptions<MasterDbContext> options) : base(options)
    {
    }

    public DbSet<TenantEntity> Tenants => Set<TenantEntity>();
    public DbSet<MasterRole> Roles => Set<MasterRole>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(MasterDbContext).Assembly,
            type => type.Namespace?.Contains("Master") == true);

        // Global Query Filter для Soft Delete
        modelBuilder.Entity<TenantEntity>().HasQueryFilter(t => !t.IsDeleted);
    }
}

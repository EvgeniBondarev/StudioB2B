using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Domain.Entities;

namespace StudioB2B.Infrastructure.Persistence.Master;

public class MasterDbContext : DbContext
{
    public MasterDbContext(DbContextOptions<MasterDbContext> options) : base(options)
    {
    }

    public DbSet<TenantEntity> Tenants => Set<TenantEntity>();
    public DbSet<MasterRole> Roles => Set<MasterRole>();
    public DbSet<MasterUser> Users => Set<MasterUser>();
    public DbSet<MasterUserRole> UserRoles => Set<MasterUserRole>();
    public DbSet<TenantBackupSchedule> TenantBackupSchedules => Set<TenantBackupSchedule>();
    public DbSet<TenantBackupHistory> TenantBackupHistories => Set<TenantBackupHistory>();
    public DbSet<TenantRestoreHistory> TenantRestoreHistories => Set<TenantRestoreHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);


        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(MasterDbContext).Assembly,
            type => type.Namespace?.Contains("Master") == true);

        ApplySoftDeleteFilters(modelBuilder);
    }

    private static void ApplySoftDeleteFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
                continue;

            var param = Expression.Parameter(entityType.ClrType, "e");
            var isDeletedProp = Expression.Property(param, nameof(ISoftDelete.IsDeleted));
            var notDeleted = Expression.Not(isDeletedProp);
            var lambda = Expression.Lambda(notDeleted, param);

            entityType.SetQueryFilter(lambda);
        }
    }
}

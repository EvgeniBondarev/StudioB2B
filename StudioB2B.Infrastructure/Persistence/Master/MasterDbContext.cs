using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
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
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
                continue;

            ParameterExpression param = Expression.Parameter(entityType.ClrType, "e");
            MemberExpression isDeletedProp = Expression.Property(param, nameof(ISoftDelete.IsDeleted));
            UnaryExpression notDeleted = Expression.Not(isDeletedProp);
            LambdaExpression lambda = Expression.Lambda(notDeleted, param);

            entityType.SetQueryFilter(lambda);
        }
    }
}

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Domain.Entities.Tenants;
using StudioB2B.Infrastructure.Persistence.Master;

namespace StudioB2B.Infrastructure.Features;

public static class TenantExtensions
{
    public static Task<List<TenantEntity>> GetAllAsync(
        this IQueryable<TenantEntity> q,
        Expression<Func<TenantEntity, bool>>? predicate = null,
        CancellationToken ct = default)
    {
        q = q.AsNoTracking();

        if (predicate != null)
            q = q.Where(predicate);

        return q.ToListAsync(ct);
    }

    public static Task<TenantEntity?> GetByPredicateAsync(
        this IQueryable<TenantEntity> q,
        Expression<Func<TenantEntity, bool>> predicate,
        CancellationToken ct = default)
    {
        return q.AsNoTracking()
            .FirstOrDefaultAsync(predicate, ct);
    }

    public static async Task<TenantEntity> CreateAsync(
        this MasterDbContext db,
        TenantEntity entity,
        CancellationToken ct = default)
    {
        db.Set<TenantEntity>().Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public static async Task<TenantEntity?> UpdateAsync(
        this MasterDbContext db,
        TenantEntity entity,
        CancellationToken ct = default)
    {
        if (!await db.Set<TenantEntity>().AnyAsync(e => e.Id == entity.Id, ct))
            return null;

        db.Set<TenantEntity>().Update(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public static async Task<bool> DeleteAsync(
        this MasterDbContext db,
        Guid id,
        CancellationToken ct = default)
    {
        var entity = await db.Set<TenantEntity>().FindAsync([id], ct);
        if (entity == null)
            return false;

        db.Set<TenantEntity>().Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

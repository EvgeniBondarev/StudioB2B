using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Domain.Entities.Tenant;
using StudioB2B.Infrastructure.Persistence.Tenant;

namespace StudioB2B.Infrastructure.Features;

public static class TenantRoleExtensions
{
    public static Task<List<TenantRole>> GetAllAsync(
        this IQueryable<TenantRole> q,
        Expression<Func<TenantRole, bool>>? predicate = null,
        CancellationToken ct = default)
    {
        q = q.AsNoTracking();

        if (predicate != null)
            q = q.Where(predicate);

        return q.ToListAsync(ct);
    }

    public static Task<TenantRole?> GetByPredicateAsync(
        this IQueryable<TenantRole> q,
        Expression<Func<TenantRole, bool>> predicate,
        CancellationToken ct = default)
    {
        return q.AsNoTracking()
            .FirstOrDefaultAsync(predicate, ct);
    }

    public static async Task<TenantRole> CreateAsync(
        this TenantDbContext db,
        TenantRole entity,
        CancellationToken ct = default)
    {
        db.Set<TenantRole>().Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public static async Task<TenantRole?> UpdateAsync(
        this TenantDbContext db,
        TenantRole entity,
        CancellationToken ct = default)
    {
        if (!await db.Set<TenantRole>().AnyAsync(e => e.Id == entity.Id, ct))
            return null;

        db.Set<TenantRole>().Update(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public static async Task<bool> DeleteAsync(
        this DbContext db,
        Guid id,
        CancellationToken ct = default)
    {
        var entity = await db.Set<TenantRole>().FindAsync([id], ct);
        if (entity == null)
            return false;

        db.Set<TenantRole>().Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

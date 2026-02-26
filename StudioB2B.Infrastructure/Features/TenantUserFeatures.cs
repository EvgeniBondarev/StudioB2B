using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Domain.Entities.Tenant;
using StudioB2B.Infrastructure.Persistence.Tenant;

namespace StudioB2B.Infrastructure.Features;

public static class TenantUserExtensions
{
    public static IQueryable<TenantUser> IncludeEverything(this IQueryable<TenantUser> q)
    {
        return q.Include(c => c.Roles);
    }

    public static Task<List<TenantUser>> GetAllAsync(
        this IQueryable<TenantUser> q,
        Expression<Func<TenantUser, bool>>? predicate = null,
        CancellationToken ct = default)
    {
        q = q.AsNoTracking()
            .IncludeEverything();

        if (predicate != null)
            q = q.Where(predicate);

        return q.ToListAsync(ct);
    }

    public static Task<TenantUser?> GetByPredicateAsync(
        this IQueryable<TenantUser> q,
        Expression<Func<TenantUser, bool>> predicate,
        CancellationToken ct = default)
    {
        return q.AsNoTracking()
            .IncludeEverything()
            .FirstOrDefaultAsync(predicate, ct);
    }

    public static async Task<TenantUser> CreateAsync(
        this TenantDbContext db,
        TenantUser entity,
        CancellationToken ct = default)
    {
        db.Set<TenantUser>().Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public static async Task<TenantUser?> UpdateAsync(
        this TenantDbContext db,
        TenantUser entity,
        CancellationToken ct = default)
    {
        if (!await db.Set<TenantUser>().AnyAsync(e => e.Id == entity.Id, ct))
            return null;

        db.Set<TenantUser>().Update(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public static async Task<bool> DeleteAsync(
        this TenantDbContext db,
        Guid id,
        CancellationToken ct = default)
    {
        var entity = await db.Set<TenantUser>().FindAsync([id], ct);
        if (entity == null)
            return false;

        db.Set<TenantUser>().Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

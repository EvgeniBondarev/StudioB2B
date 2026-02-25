using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Domain.Entities.Common;

namespace StudioB2B.Infrastructure.Features;

public static class RoleExtensions
{
    public static Task<List<Role>> GetAllAsync(
        this IQueryable<Role> q,
        Expression<Func<Role, bool>>? predicate = null,
        CancellationToken ct = default)
    {
        q = q.AsNoTracking();

        if (predicate != null)
            q = q.Where(predicate);

        return q.ToListAsync(ct);
    }

    public static Task<Role?> GetByPredicateAsync(
        this IQueryable<Role> q,
        Expression<Func<Role, bool>> predicate,
        CancellationToken ct = default)
    {
        return q.AsNoTracking()
            .FirstOrDefaultAsync(predicate, ct);
    }

    public static async Task<Role> CreateAsync(
        this DbContext db,
        Role entity,
        CancellationToken ct = default)
    {
        db.Set<Role>().Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public static async Task<Role?> UpdateAsync(
        this DbContext db,
        Role entity,
        CancellationToken ct = default)
    {
        if (!await db.Set<Role>().AnyAsync(e => e.Id == entity.Id, ct))
            return null;

        db.Set<Role>().Update(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public static async Task<bool> DeleteAsync(
        this DbContext db,
        Guid id,
        CancellationToken ct = default)
    {
        var entity = await db.Set<Role>().FindAsync([id], ct);
        if (entity == null)
            return false;

        db.Set<Role>().Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Domain.Entities.Master;
using StudioB2B.Infrastructure.Persistence.Master;

namespace StudioB2B.Infrastructure.Features;

public static class MasterRoleExtensions
{
    public static Task<List<MasterRole>> GetAllAsync(
        this IQueryable<MasterRole> q,
        Expression<Func<MasterRole, bool>>? predicate = null,
        CancellationToken ct = default)
    {
        q = q.AsNoTracking();

        if (predicate != null)
            q = q.Where(predicate);

        return q.ToListAsync(ct);
    }

    public static Task<MasterRole?> GetByPredicateAsync(
        this IQueryable<MasterRole> q,
        Expression<Func<MasterRole, bool>> predicate,
        CancellationToken ct = default)
    {
        return q.AsNoTracking()
            .FirstOrDefaultAsync(predicate, ct);
    }

    public static async Task<MasterRole> CreateAsync(
        this MasterDbContext db,
        MasterRole entity,
        CancellationToken ct = default)
    {
        db.Set<MasterRole>().Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public static async Task<MasterRole?> UpdateAsync(
        this MasterDbContext db,
        MasterRole entity,
        CancellationToken ct = default)
    {
        if (!await db.Set<MasterRole>().AnyAsync(e => e.Id == entity.Id, ct))
            return null;

        db.Set<MasterRole>().Update(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public static async Task<bool> DeleteAsync(
        this DbContext db,
        Guid id,
        CancellationToken ct = default)
    {
        var entity = await db.Set<MasterRole>().FindAsync([id], ct);
        if (entity == null)
            return false;

        db.Set<MasterRole>().Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Domain.Entities.Common;
using StudioB2B.Domain.Entities.Master;
using StudioB2B.Infrastructure.Persistence.Master;

namespace StudioB2B.Infrastructure.Features;


public static class UserExtensions
{
    public static IQueryable<MasterUser> IncludeEverything(this IQueryable<MasterUser> q)
    {
        return q.Include(c => c.Roles);
    }

    public static Task<List<MasterUser>> GetAllAsync(
        this IQueryable<MasterUser> q,
        Expression<Func<MasterUser, bool>>? predicate = null,
        CancellationToken ct = default)
    {
        q = q.AsNoTracking()
            .IncludeEverything();

        if (predicate != null)
            q = q.Where(predicate);

        return q.ToListAsync(ct);
    }

    public static Task<MasterUser?> GetByPredicateAsync(
        this IQueryable<MasterUser> q,
        Expression<Func<MasterUser, bool>> predicate,
        CancellationToken ct = default)
    {
        return q.AsNoTracking()
            .IncludeEverything()
            .FirstOrDefaultAsync(predicate, ct);
    }

    public static async Task<MasterUser> CreateAsync(
        this MasterDbContext db,
        MasterUser entity,
        CancellationToken ct = default)
    {
        db.Set<MasterUser>().Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public static async Task<MasterUser?> UpdateAsync(
        this MasterDbContext db,
        MasterUser entity,
        CancellationToken ct = default)
    {
        if (!await db.Set<MasterUser>().AnyAsync(e => e.Id == entity.Id, ct))
            return null;

        db.Set<MasterUser>().Update(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public static async Task<bool> DeleteAsync(
        this MasterDbContext db,
        Guid id,
        CancellationToken ct = default)
    {
        var entity = await db.Set<MasterUser>().FindAsync([id], ct);
        if (entity == null)
            return false;

        db.Set<MasterUser>().Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

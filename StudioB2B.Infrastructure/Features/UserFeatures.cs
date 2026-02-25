using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Domain.Entities.Common;
using StudioB2B.Infrastructure.Persistence.Tenant;

namespace StudioB2B.Infrastructure.Features;


public static class UserExtensions
{
    public static IQueryable<User> IncludeEverything(this IQueryable<User> q)
    {
        return q.Include(c => c.Roles);
    }

    public static Task<List<User>> GetAllAsync(
        this IQueryable<User> q,
        Expression<Func<User, bool>>? predicate = null,
        CancellationToken ct = default)
    {
        q = q.AsNoTracking()
            .IncludeEverything();

        if (predicate != null)
            q = q.Where(predicate);

        return q.ToListAsync(ct);
    }

    public static Task<User?> GetByPredicateAsync(
        this IQueryable<User> q,
        Expression<Func<User, bool>> predicate,
        CancellationToken ct = default)
    {
        return q.AsNoTracking()
            .IncludeEverything()
            .FirstOrDefaultAsync(predicate, ct);
    }

    public static async Task<User> CreateAsync(
        this TenantDbContext db,
        User entity,
        CancellationToken ct = default)
    {
        db.Set<User>().Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public static async Task<User?> UpdateAsync(
        this TenantDbContext db,
        User entity,
        CancellationToken ct = default)
    {
        if (!await db.Set<User>().AnyAsync(e => e.Id == entity.Id, ct))
            return null;

        db.Set<User>().Update(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public static async Task<bool> DeleteAsync(
        this TenantDbContext db,
        Guid id,
        CancellationToken ct = default)
    {
        var entity = await db.Set<User>().FindAsync([id], ct);
        if (entity == null)
            return false;

        db.Users.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Domain.Entities.Marketplace;
using StudioB2B.Infrastructure.Persistence.Tenant;

namespace StudioB2B.Infrastructure.Features;

public static class MarketplaceClientExtensions
{
    public static IQueryable<MarketplaceClient> IncludeEverything(this IQueryable<MarketplaceClient> q)
    {
        return q.Include(c => c.ClientType)
            .Include(c => c.Mode)
            .Include(c => c.Settings)
            .Include(c => c.Settings1C);
    }

    public static Task<List<MarketplaceClient>> GetAllAsync(
        this IQueryable<MarketplaceClient> q,
        Expression<Func<MarketplaceClient, bool>>? predicate = null,
        CancellationToken ct = default)
    {
        q = q.AsNoTracking()
            .IncludeEverything();

        if (predicate != null)
            q = q.Where(predicate);

        return q.ToListAsync(ct);
    }

    public static Task<MarketplaceClient?> GetByPredicateAsync(
        this IQueryable<MarketplaceClient> q,
        Expression<Func<MarketplaceClient, bool>> predicate,
        CancellationToken ct = default)
    {
        return q.AsNoTracking()
            .IncludeEverything()
            .FirstOrDefaultAsync(predicate, ct);
    }

    public static async Task<MarketplaceClient> CreateAsync(
        this TenantDbContext db,
        MarketplaceClient entity,
        CancellationToken ct = default)
    {
        db.MarketplaceClients.Add(entity!);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public static async Task<MarketplaceClient?> UpdateAsync(
        this TenantDbContext db,
        MarketplaceClient entity,
        CancellationToken ct = default)
    {
        if (!await db.Set<MarketplaceClient>().AnyAsync(e => e.Id == entity.Id, ct))
            return null;

        db.Set<MarketplaceClient>().Update(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public static async Task<bool> DeleteAsync(
        this TenantDbContext db,
        Guid id,
        CancellationToken ct = default)
    {
        var entity = await db.Set<MarketplaceClient>().FindAsync([id], ct);
        if (entity == null)
            return false;

        db.Set<MarketplaceClient>().Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

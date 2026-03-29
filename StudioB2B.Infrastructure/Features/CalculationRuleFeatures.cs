using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Persistence.Tenant;

namespace StudioB2B.Infrastructure.Features;

public static class CalculationRuleExtensions
{
    public static IQueryable<CalculationRule> Active(this IQueryable<CalculationRule> q)
        => q.Where(r => r.IsActive && !r.IsDeleted);

    public static Task<List<CalculationRule>> GetActiveRulesAsync(this TenantDbContext db, CancellationToken ct = default)
        => db.CalculationRules.AsQueryable().Active().AsNoTracking().OrderBy(r => r.SortOrder).ToListAsync(ct);

    public static async Task<(List<CalculationRule> Items, int Total)> GetCalculationRulesPagedAsync(
        this TenantDbContext db,
        string? filter,
        string? orderBy,
        int skip,
        int take,
        CancellationToken ct = default)
    {
        var query = db.CalculationRules.Where(r => !r.IsDeleted).AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(filter))
            query = query.Where(filter);
        var total = await query.CountAsync(ct);
        query = !string.IsNullOrEmpty(orderBy)
            ? query.OrderBy(orderBy)
            : query.OrderBy(r => r.SortOrder).ThenBy(r => r.Name);
        return (await query.Skip(skip).Take(take).ToListAsync(ct), total);
    }

    public static Task<List<string>> GetPriceTypeNamesAsync(this TenantDbContext db, CancellationToken ct = default)
        => db.PriceTypes.AsNoTracking().OrderBy(p => p.Name).Select(p => p.Name).ToListAsync(ct);

    public static async Task<int> GetNextCalculationRuleSortOrderAsync(this TenantDbContext db, CancellationToken ct = default)
    {
        var max = await db.CalculationRules.Where(r => !r.IsDeleted).MaxAsync(r => (int?)r.SortOrder, ct);
        return (max ?? 0) + 1;
    }

    public static async Task<OrderEntity?> GetExampleOrderAsync(this TenantDbContext db, string? postingNumber, CancellationToken ct = default)
    {
        var q = db.Orders
            .Include(o => o.Shipment)
            .Include(o => o.Prices).ThenInclude(p => p.PriceType)
            .Include(o => o.ProductInfo).ThenInclude(pi => pi!.Product)
            .AsNoTracking();
        if (!string.IsNullOrWhiteSpace(postingNumber))
        {
            var match = await q.FirstOrDefaultAsync(o => o.Shipment.PostingNumber == postingNumber.Trim(), ct);
            if (match != null) return match;
        }
        return await q.OrderByDescending(o => o.Id).FirstOrDefaultAsync(ct);
    }

    public static async Task<CalculationRule> CreateCalculationRuleAsync(this TenantDbContext db, CalculationRule rule, CancellationToken ct = default)
    {
        db.CalculationRules.Add(rule);
        await db.SaveChangesAsync(ct);
        return rule;
    }

    public static async Task UpdateCalculationRuleAsync(this TenantDbContext db, CalculationRule rule, CancellationToken ct = default)
    {
        if (!db.CalculationRules.Local.Contains(rule))
            db.CalculationRules.Attach(rule).State = EntityState.Modified;
        await db.SaveChangesAsync(ct);
    }

    public static async Task SoftDeleteCalculationRuleAsync(this TenantDbContext db, CalculationRule rule, CancellationToken ct = default)
    {
        rule.IsDeleted = true;
        if (!db.CalculationRules.Local.Contains(rule))
            db.CalculationRules.Attach(rule).State = EntityState.Modified;
        await db.SaveChangesAsync(ct);
    }
}


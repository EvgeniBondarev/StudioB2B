using Microsoft.EntityFrameworkCore;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Persistence.Tenant;

namespace StudioB2B.Infrastructure.Features.Orders;

public static class CalculationRuleExtensions
{
    /// <summary>
    /// Только активные и не удалённые правила.
    /// </summary>
    public static IQueryable<CalculationRule> Active(this IQueryable<CalculationRule> q)
    {
        return q.Where(r => r.IsActive && !r.IsDeleted);
    }

    /// <summary>
    /// Загрузить все активные правила, отсортированные по SortOrder.
    /// </summary>
    public static Task<List<CalculationRule>> GetActiveRulesAsync(
        this TenantDbContext db,
        CancellationToken ct = default)
    {
        return db.CalculationRules
            .AsQueryable()
            .Active()
            .AsNoTracking()
            .OrderBy(r => r.SortOrder)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Создание нового правила.
    /// </summary>
    public static async Task<CalculationRule> CreateCalculationRuleAsync(
        this TenantDbContext db,
        CalculationRule rule,
        CancellationToken ct = default)
    {
        db.CalculationRules.Add(rule);
        await db.SaveChangesAsync(ct);
        return rule;
    }

    /// <summary>
    /// Обновление существующего правила.
    /// </summary>
    public static async Task UpdateCalculationRuleAsync(
        this TenantDbContext db,
        CalculationRule rule,
        CancellationToken ct = default)
    {
        if (!db.CalculationRules.Local.Contains(rule))
        {
            db.CalculationRules.Attach(rule).State = EntityState.Modified;
        }

        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Мягкое удаление правила (IsDeleted = true).
    /// </summary>
    public static async Task SoftDeleteCalculationRuleAsync(
        this TenantDbContext db,
        CalculationRule rule,
        CancellationToken ct = default)
    {
        rule.IsDeleted = true;

        if (!db.CalculationRules.Local.Contains(rule))
        {
            db.CalculationRules.Attach(rule).State = EntityState.Modified;
        }

        await db.SaveChangesAsync(ct);
    }
}

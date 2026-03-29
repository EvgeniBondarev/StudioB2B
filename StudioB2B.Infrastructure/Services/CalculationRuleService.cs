using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Features;
using StudioB2B.Infrastructure.Interfaces;

namespace StudioB2B.Infrastructure.Services;

/// <summary>
/// Сервис для работы с правилами расчёта тенанта.
/// Инкапсулирует работу с БД, используя extension-методы из CalculationRuleFeatures.
/// </summary>
public class CalculationRuleService : ICalculationRuleService
{
    private readonly ITenantDbContextFactory _dbContextFactory;

    public CalculationRuleService(ITenantDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<(List<CalculationRule> Items, int Total)> GetPagedAsync(
        string? filter,
        string? orderBy,
        int skip,
        int take,
        CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.GetCalculationRulesPagedAsync(filter, orderBy, skip, take, ct);
    }

    public async Task<List<string>> GetAvailableVariablesAsync(CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        var priceTypeNames = await db.GetPriceTypeNamesAsync(ct);
        return CalculationEngine.GetBaseVariableNames(priceTypeNames).ToList();
    }

    public async Task<int> GetNextSortOrderAsync(CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.GetNextCalculationRuleSortOrderAsync(ct);
    }

    public async Task<OrderEntity?> GetExampleOrderAsync(string? postingNumber, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.GetExampleOrderAsync(postingNumber, ct);
    }

    public async Task<List<CalculationRule>> GetActiveRulesAsync(CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.GetActiveRulesAsync(ct);
    }

    public async Task<CalculationRule> CreateAsync(CalculationRule rule, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.CreateCalculationRuleAsync(rule, ct);
    }

    public async Task UpdateAsync(CalculationRule rule, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        await db.UpdateCalculationRuleAsync(rule, ct);
    }

    public async Task SoftDeleteAsync(CalculationRule rule, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        await db.SoftDeleteCalculationRuleAsync(rule, ct);
    }
}


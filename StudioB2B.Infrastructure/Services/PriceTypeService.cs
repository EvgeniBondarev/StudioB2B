using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Features;
using StudioB2B.Infrastructure.Interfaces;

namespace StudioB2B.Infrastructure.Services;

/// <summary>
/// Сервис типов цен: инкапсулирует все запросы к БД,
/// использует extension-методы из PriceTypeFeatures.
/// </summary>
public class PriceTypeService : IPriceTypeService
{
    private readonly ITenantDbContextFactory _dbContextFactory;

    public PriceTypeService(ITenantDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    /// <inheritdoc/>
    public async Task<(List<PriceType> Items, int TotalCount)> GetPagedAsync(
        string? dynamicFilter,
        string? orderBy,
        int skip,
        int take,
        CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.GetPriceTypesPagedAsync(dynamicFilter, orderBy, skip, take, ct);
    }

    /// <inheritdoc/>
    public async Task<PriceType> CreateAsync(PriceType item, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.CreatePriceTypeAsync(item, ct);
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateAsync(PriceType item, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.UpdatePriceTypeAsync(item, ct);
    }

    /// <inheritdoc/>
    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.SoftDeletePriceTypeAsync(id, ct);
    }
}


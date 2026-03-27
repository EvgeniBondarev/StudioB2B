using StudioB2B.Infrastructure.Features;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Shared.DTOs;

namespace StudioB2B.Infrastructure.Services;

/// <summary>
/// Сервис возвратов: инкапсулирует все запросы к БД страницы Returns
/// и делегирует работу extension-методам из ReturnFeatures.
/// </summary>
public class ReturnsService : IReturnsService
{
    private readonly ITenantDbContextFactory _dbContextFactory;

    public ReturnsService(ITenantDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    /// <inheritdoc/>
    public async Task<ReturnsCountsData> GetCountsAsync(CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.GetReturnsCountsAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<ReturnsPageResult> GetPageAsync(ReturnsPageRequest request, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.GetReturnsPageAsync(request, ct);
    }
}


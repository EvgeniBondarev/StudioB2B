using AutoMapper;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Domain.Constants;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Features;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Shared.DTOs;
using System.Linq.Dynamic.Core;

namespace StudioB2B.Infrastructure.Services;

/// <summary>
/// Сервис для работы с клиентами маркетплейсов.
/// Инкапсулирует работу с БД и фильтрацию по правам пользователя.
/// </summary>
public class MarketplaceClientService : IMarketplaceClientService
{
    private readonly ITenantDbContextFactory _dbContextFactory;
    private readonly IEntityFilterService _entityFilterService;
    private readonly IMapper _mapper;

    public MarketplaceClientService(
        ITenantDbContextFactory dbContextFactory,
        IEntityFilterService entityFilterService,
        IMapper mapper)
    {
        _dbContextFactory = dbContextFactory;
        _entityFilterService = entityFilterService;
        _mapper = mapper;
    }

    /// <inheritdoc/>
    public async Task<MarketplaceClientInitData> GetInitDataAsync(CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.GetInitDataAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<(List<MarketplaceClient> Items, int Total)> GetPagedAsync(
        MarketplaceClientPageFilter filter, int skip, int take, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();

        var query = db.MarketplaceClients!
            .IncludeEverything()
            .AsNoTracking()
            .AsQueryable();

        if (filter.TypeId.HasValue)
            query = query.Where(c => c.ClientTypeId == filter.TypeId.Value);
        if (filter.ModeId.HasValue)
            query = query.Where(c => c.ModeId == filter.ModeId.Value || c.ModeId2 == filter.ModeId.Value);
        if (!string.IsNullOrEmpty(filter.Filter))
            query = query.Where(filter.Filter);

        var total = await query.CountAsync(ct);

        if (!string.IsNullOrEmpty(filter.OrderBy))
            query = query.OrderBy(filter.OrderBy);
        else
            query = query.OrderBy(c => c.Name);

        var items = await query.Skip(skip).Take(take).ToListAsync(ct);
        return (items, total);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(MarketplaceClient client, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        db.MarketplaceClients!.Update(client);
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        await db.DeleteAsync(id, ct);
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsByApiIdAsync(string apiId, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.MarketplaceClients!
            .AsNoTracking()
            .AnyAsync(c => c.ApiId == apiId, ct);
    }

    /// <inheritdoc/>
    public async Task<MarketplaceClientDto> CreateAsync(CreateMarketplaceClientDto dto, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.CreateAsync(dto, _mapper, ct);
    }

    /// <inheritdoc/>
    public async Task<bool> HasAnyAsync(CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.MarketplaceClients!.AnyAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<List<ClientOptionDto>> GetClientOptionsAsync(CancellationToken ct = default)
    {
        var allowedIds = await _entityFilterService
            .GetAllowedIdsAsync(BlockedEntityTypeEnum.MarketplaceClient, ct);

        using var db = _dbContextFactory.CreateDbContext();
        return await db.GetClientOptionsAsync(allowedIds, ct);
    }
}

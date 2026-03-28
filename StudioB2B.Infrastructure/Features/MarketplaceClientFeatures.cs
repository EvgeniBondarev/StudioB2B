using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Persistence.Tenant;
using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Features;

public static class MarketplaceClientExtensions
{
    public static IQueryable<MarketplaceClient> IncludeEverything(this IQueryable<MarketplaceClient> q)
    {
        return q
            .Include(c => c.ClientType)
            .Include(c => c.Mode)
            .Include(c => c.Mode2)
            .Include(c => c.Settings)
            .Include(c => c.Settings1C);
    }

    public static async Task<List<MarketplaceClientDto>> GetAllAsync(
        this IQueryable<MarketplaceClient> q,
        IMapper mapper,
        CancellationToken ct = default)
    {
        // We avoid ProjectTo here because ModeIds/ModeNames is projected as an in-memory list.
        var entities = await q
            .AsNoTracking()
            .IncludeEverything()
            .ToListAsync(ct);

        return entities
            .Select(e => mapper.Map<MarketplaceClientDto>(e))
            .ToList();
    }

    // Paging logic moved to web project for MudBlazor dependency
    public static IQueryable<MarketplaceClientDto> ProjectToDto(
        this IQueryable<MarketplaceClient> q,
        IMapper mapper)
    {
        return q
            .AsNoTracking()
            .IncludeEverything()
            .ProjectTo<MarketplaceClientDto>(mapper.ConfigurationProvider);
    }

    public static async Task<MarketplaceClientDto?> GetByIdAsync(
        this IQueryable<MarketplaceClient> q,
        Guid id,
        IMapper mapper,
        CancellationToken ct = default)
    {
        var entity = await q
            .AsNoTracking()
            .IncludeEverything()
            .Where(c => c.Id == id)
            .FirstOrDefaultAsync(ct)
            ;

        return entity != null ? mapper.Map<MarketplaceClientDto>(entity) : null;
    }

    public static async Task<MarketplaceClientDto> CreateAsync(
        this TenantDbContext db,
        CreateMarketplaceClientDto request,
        IMapper mapper,
        CancellationToken ct = default)
    {
        var entity = mapper.Map<MarketplaceClient>(request);
        db.MarketplaceClients!.Add(entity!);
        await db.SaveChangesAsync(ct);
        return mapper.Map<MarketplaceClientDto>(entity);
    }

    public static async Task<MarketplaceClientDto?> UpdateAsync(
        this TenantDbContext db,
        UpdateMarketplaceClientDto request,
        IMapper mapper,
        CancellationToken ct = default)
    {
        var entity = await db.MarketplaceClients!.FindAsync(new object[] { request.Id }, ct);
        if (entity == null) return null;
        mapper.Map(request, entity);
        await db.SaveChangesAsync(ct);
        return mapper.Map<MarketplaceClientDto>(entity);
    }

    public static async Task<bool> DeleteAsync(
        this TenantDbContext db,
        Guid id,
        CancellationToken ct = default)
    {
        var entity = await db.MarketplaceClients!.FindAsync(new object[] { id }, ct);
        if (entity == null) return false;
        db.MarketplaceClients.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public static async Task<List<ClientOptionDto>> GetClientOptionsAsync(
        this TenantDbContext db,
        ICollection<Guid>? allowedIds,
        CancellationToken ct = default)
    {
        var query = db.MarketplaceClients!.AsNoTracking().AsQueryable();
        if (allowedIds is not null)
            query = query.Where(c => allowedIds.Contains(c.Id));
        return await query
            .OrderBy(c => c.Name)
            .Select(c => new ClientOptionDto { Id = c.Id, Name = c.Name ?? string.Empty })
            .ToListAsync(ct);
    }

    public static async Task<MarketplaceClientInitData> GetInitDataAsync(
        this TenantDbContext db,
        CancellationToken ct = default)
    {
        var types = await db.MarketplaceClientTypes!
            .OrderBy(t => t.Name).AsNoTracking().ToListAsync(ct);
        var modes = await db.MarketplaceClientModes!
            .OrderBy(m => m.Name).AsNoTracking().ToListAsync(ct);
        var countsByType = await db.MarketplaceClients!
            .Where(c => c.ClientTypeId != null)
            .GroupBy(c => c.ClientTypeId!.Value)
            .Select(g => new { TypeId = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var countsByMode = await db.MarketplaceClients!
            .Where(c => c.ModeId.HasValue)
            .GroupBy(c => c.ModeId!.Value)
            .Select(g => new { ModeId = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        return new MarketplaceClientInitData(
            types,
            modes,
            countsByType.ToDictionary(x => x.TypeId, x => x.Count),
            countsByMode.ToDictionary(x => x.ModeId, x => x.Count));
    }
}

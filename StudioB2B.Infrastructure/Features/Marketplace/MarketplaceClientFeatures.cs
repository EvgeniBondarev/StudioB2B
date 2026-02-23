using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Infrastructure.Persistence.Tenant;
using StudioB2B.Shared.DTOs;
using StudioB2B.Domain.Entities.Marketplace;

namespace StudioB2B.Infrastructure.Features.Marketplace;

public static class MarketplaceClientExtensions
{
    public static IQueryable<MarketplaceClient> IncludeEverything(this IQueryable<MarketplaceClient> q)
    {
        return q
            .Include(c => c.ClientType)
            .Include(c => c.Mode)
            .Include(c => c.Settings)
            .Include(c => c.Settings1C);
    }

    public static Task<List<MarketplaceClientDto>> GetAllAsync(
        this IQueryable<MarketplaceClient> q,
        IMapper mapper,
        CancellationToken ct = default)
    {
        return q
                 .AsNoTracking()
                 .IncludeEverything()
                 .ProjectTo<MarketplaceClientDto>(mapper.ConfigurationProvider)
                 .ToListAsync(ct);
    }

    public static Task<MarketplaceClientDto?> GetByIdAsync(
        this IQueryable<MarketplaceClient> q,
        Guid id,
        IMapper mapper,
        CancellationToken ct = default)
    {
        return q
                 .AsNoTracking()
                 .IncludeEverything()
                 .Where(c => c.Id == id)
                 .ProjectTo<MarketplaceClientDto>(mapper.ConfigurationProvider)
                 .FirstOrDefaultAsync(ct);
    }


    public static async Task<MarketplaceClientDto> CreateAsync(
        this TenantDbContext db,
        CreateMarketplaceClientRequest request,
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
        UpdateMarketplaceClientRequest request,
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
}

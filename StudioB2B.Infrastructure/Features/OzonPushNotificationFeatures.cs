using Microsoft.EntityFrameworkCore;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Persistence.Tenant;
using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Features;

public static class OzonPushNotificationFeatures
{
    public static async Task<OzonPushNotification> SaveAsync(
        this TenantDbContext db,
        string messageType,
        string rawPayload,
        long? sellerId,
        string? postingNumber,
        Guid? marketplaceClientId,
        CancellationToken ct = default)
    {
        var entity = new OzonPushNotification
        {
            Id = Guid.NewGuid(),
            MessageType = messageType,
            RawPayload = rawPayload,
            SellerId = sellerId,
            PostingNumber = postingNumber,
            ReceivedAtUtc = DateTime.UtcNow,
            MarketplaceClientId = marketplaceClientId
        };

        db.OzonPushNotifications.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public static async Task<OzonPushPageResult> GetPageAsync(
        this TenantDbContext db,
        OzonPushPageRequest request,
        CancellationToken ct = default)
    {
        var query = db.OzonPushNotifications
            .AsNoTracking()
            .Include(x => x.MarketplaceClient);

        IQueryable<OzonPushNotification> filtered = query;

        if (!string.IsNullOrWhiteSpace(request.MessageType))
            filtered = filtered.Where(x => x.MessageType == request.MessageType);

        if (request.MarketplaceClientId.HasValue)
            filtered = filtered.Where(x => x.MarketplaceClientId == request.MarketplaceClientId.Value);

        if (request.From.HasValue)
            filtered = filtered.Where(x => x.ReceivedAtUtc >= request.From.Value);

        if (request.To.HasValue)
            filtered = filtered.Where(x => x.ReceivedAtUtc <= request.To.Value);

        var total = await filtered.CountAsync(ct);

        var items = await filtered
            .OrderByDescending(x => x.ReceivedAtUtc)
            .Skip(request.Skip)
            .Take(request.Take)
            .Select(x => new OzonPushNotificationDto
            {
                Id = x.Id,
                MessageType = x.MessageType,
                RawPayload = x.RawPayload,
                SellerId = x.SellerId,
                PostingNumber = x.PostingNumber,
                ReceivedAtUtc = x.ReceivedAtUtc,
                MarketplaceClientId = x.MarketplaceClientId,
                MarketplaceClientName = x.MarketplaceClient != null ? x.MarketplaceClient.Name : null
            })
            .ToListAsync(ct);

        return new OzonPushPageResult { Items = items, TotalCount = total };
    }

    public static async Task<int> DeleteAllAsync(
        this TenantDbContext db,
        CancellationToken ct = default)
    {
        return await db.OzonPushNotifications.ExecuteDeleteAsync(ct);
    }

    /// <summary>
    /// Ищет MarketplaceClient по ApiId == sellerId.ToString().
    /// </summary>
    public static async Task<Guid?> FindClientIdBySellerIdAsync(
        this TenantDbContext db,
        long sellerId,
        CancellationToken ct = default)
    {
        var sellerStr = sellerId.ToString();
        var client = await db.MarketplaceClients!
            .AsNoTracking()
            .Where(c => c.ApiId == sellerStr)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(ct);
        return client;
    }
}


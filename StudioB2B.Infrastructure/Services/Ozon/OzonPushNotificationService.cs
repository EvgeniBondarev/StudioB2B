using Microsoft.Extensions.Logging;
using StudioB2B.Infrastructure.Features;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Infrastructure.Persistence.Tenant;
using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Services.Ozon;

public class OzonPushNotificationService : IOzonPushNotificationService
{
    private readonly ITenantDbContextFactory _dbContextFactory;
    private readonly ILogger<OzonPushNotificationService> _logger;

    public OzonPushNotificationService(ITenantDbContextFactory dbContextFactory, ILogger<OzonPushNotificationService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task<OzonPushNotificationDto> SaveAsync(
        string messageType,
        string rawPayload,
        long? sellerId,
        string? postingNumber,
        string? chatId,
        string? messageText,
        CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();

        Guid? clientId = null;
        if (sellerId.HasValue)
        {
            clientId = await db.FindClientIdBySellerIdAsync(sellerId.Value, ct);
            if (clientId is null)
                _logger.LogWarning(
                    "OzonPush: sellerId {SellerId} not matched to any marketplace client. Message type: {MessageType}.",
                    sellerId.Value, messageType);
        }

        var entity = await db.SaveAsync(messageType, rawPayload, sellerId, postingNumber, clientId, ct);

        _logger.LogWarning(
            "OzonPush: saved {MessageType} | sellerId={SellerId} | clientId={ClientId} | posting={PostingNumber}.",
            messageType, sellerId, clientId, postingNumber);

        string? clientName = null;
        if (entity.MarketplaceClientId.HasValue)
        {
            clientName = db.MarketplaceClients!
                .Where(c => c.Id == entity.MarketplaceClientId.Value)
                .Select(c => c.Name)
                .FirstOrDefault();
        }

        return new OzonPushNotificationDto
        {
            Id = entity.Id,
            MessageType = entity.MessageType,
            RawPayload = entity.RawPayload,
            SellerId = entity.SellerId,
            PostingNumber = entity.PostingNumber,
            ChatId = chatId,
            MessageText = messageText,
            ReceivedAtUtc = entity.ReceivedAtUtc,
            MarketplaceClientId = entity.MarketplaceClientId,
            MarketplaceClientName = clientName
        };
    }

    public async Task<OzonPushPageResult> GetPageAsync(OzonPushPageRequest request, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.GetPageAsync(request, ct);
    }

    public async Task<int> DeleteAllAsync(CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.DeleteAllAsync(ct);
    }
}

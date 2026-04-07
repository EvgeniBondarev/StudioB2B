using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Interfaces;

public interface IOzonPushNotificationService
{
    Task<OzonPushNotificationDto> SaveAsync(
        string messageType,
        string rawPayload,
        long? sellerId,
        string? postingNumber,
        string? chatId,
        string? messageText,
        CancellationToken ct = default);

    Task<OzonPushPageResult> GetPageAsync(OzonPushPageRequest request, CancellationToken ct = default);

    Task<int> DeleteAllAsync(CancellationToken ct = default);
}


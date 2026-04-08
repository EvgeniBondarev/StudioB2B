using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Interfaces;

public interface IOzonPushNotificationSender
{
    Task SendPushAsync(string tenantId, OzonPushNotificationDto notification, CancellationToken ct = default);
}


namespace StudioB2B.Shared;

public class OzonPushPageResult
{
    public List<OzonPushNotificationDto> Items { get; set; } = new();

    public int TotalCount { get; set; }
}


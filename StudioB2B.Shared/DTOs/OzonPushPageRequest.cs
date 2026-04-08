namespace StudioB2B.Shared;

public class OzonPushPageRequest
{
    public string? MessageType { get; set; }

    public Guid? MarketplaceClientId { get; set; }

    public DateTime? From { get; set; }

    public DateTime? To { get; set; }

    public int Skip { get; set; }

    public int Take { get; set; } = 50;
}


using StudioB2B.Domain.Constants;

namespace StudioB2B.Shared;

public class CommunicationTaskFilter
{
    /// <summary>Single-type filter (legacy). Ignored when <see cref="TaskTypes"/> is non-empty.</summary>
    public CommunicationTaskType? TaskType { get; set; }

    /// <summary>Multi-select type filter. Empty = all types.</summary>
    public List<CommunicationTaskType> TaskTypes { get; set; } = new();

    public CommunicationTaskStatus? Status { get; set; }

    public Guid? AssignedToUserId { get; set; }

    /// <summary>Single-client filter (legacy). Ignored when <see cref="MarketplaceClientIds"/> is non-empty.</summary>
    public Guid? MarketplaceClientId { get; set; }

    /// <summary>Multi-select client filter. Empty = all clients.</summary>
    public List<Guid> MarketplaceClientIds { get; set; } = new();

    public DateTime? From { get; set; }

    public DateTime? To { get; set; }

    /// <summary>How many Done/Cancelled tasks to return in GetBoardAsync.</summary>
    public int DoneTake { get; set; } = 25;
}


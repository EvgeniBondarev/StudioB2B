using StudioB2B.Domain.Constants;

namespace StudioB2B.Shared;

public class CommunicationTaskFilter
{
    public CommunicationTaskType? TaskType { get; set; }

    public CommunicationTaskStatus? Status { get; set; }

    public Guid? AssignedToUserId { get; set; }

    public Guid? MarketplaceClientId { get; set; }

    public DateTime? From { get; set; }

    public DateTime? To { get; set; }

    /// <summary>How many Done/Cancelled tasks to return in GetBoardAsync.</summary>
    public int DoneTake { get; set; } = 25;
}


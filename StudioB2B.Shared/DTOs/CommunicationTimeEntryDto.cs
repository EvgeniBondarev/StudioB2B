namespace StudioB2B.Shared;

public class CommunicationTimeEntryDto
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public DateTime StartedAt { get; set; }

    public DateTime? EndedAt { get; set; }

    public string? Note { get; set; }

    public double DurationMinutes { get; set; }
}


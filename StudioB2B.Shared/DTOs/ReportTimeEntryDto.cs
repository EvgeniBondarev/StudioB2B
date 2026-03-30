namespace StudioB2B.Shared;

public class ReportTimeEntryDto
{
    public DateTime StartedAt { get; set; }

    public DateTime? EndedAt { get; set; }

    public double DurationMinutes { get; set; }

    public string? Note { get; set; }
}


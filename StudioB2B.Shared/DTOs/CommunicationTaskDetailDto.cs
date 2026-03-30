namespace StudioB2B.Shared;

public class CommunicationTaskDetailDto : CommunicationTaskDto
{
    public List<CommunicationTaskLogDto> Logs { get; set; } = new();

    public List<CommunicationTimeEntryDto> TimeEntries { get; set; } = new();
}


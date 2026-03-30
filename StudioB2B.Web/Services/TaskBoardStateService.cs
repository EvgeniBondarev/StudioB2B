using StudioB2B.Shared;

namespace StudioB2B.Web.Services;

/// <summary>
/// Scoped cache for task board data.
/// Survives Blazor component re-creation on navigation within the same circuit.
/// </summary>
public class TaskBoardStateService
{
    public TaskBoardDto? Board { get; private set; }

    public DateTime? LoadedAt { get; private set; }

    public bool HasData => Board is not null;

    /// <summary>True when cache is older than 5 minutes and a silent refresh is worthwhile.</summary>
    public bool IsStale => LoadedAt is null || (DateTime.UtcNow - LoadedAt.Value).TotalMinutes > 5;

    public void Set(TaskBoardDto board)
    {
        Board = board;
        LoadedAt = DateTime.UtcNow;
    }

    public void AppendDone(List<CommunicationTaskDto> items, int totalCount)
    {
        if (Board is null) return;
        Board.DoneTasks.AddRange(items);
        Board.DoneTotalCount = totalCount;
    }

    public void Invalidate()
    {
        Board = null;
        LoadedAt = null;
    }
}


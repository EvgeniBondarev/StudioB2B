using StudioB2B.Domain.Constants;
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

    public string? ActivePreviewExternalId { get; private set; }

    public Guid? ActivePreviewClientId { get; private set; }

    public CommunicationTaskType? ActivePreviewType { get; private set; }

    /// <summary>
    /// Called by ShowPreviewAsync to track the currently open item so it can be
    /// restored when the user navigates away and back. Does NOT fire any event.
    /// </summary>
    public void SetActivePreview(string externalId, Guid clientId, CommunicationTaskType type)
    {
        ActivePreviewExternalId = externalId;
        ActivePreviewClientId = clientId;
        ActivePreviewType = type;
    }

    // ── Notification-driven chat open ──────────────────────────

    private string? _pendingChatId;
    private Guid? _pendingChatClientId;

    /// <summary>Fires when a push notification requests that a specific chat be opened.</summary>
    public event Action? PendingChatOpenChanged;

    /// <summary>
    /// Called from MainLayout when a chat push notification is clicked.
    /// Stores the request and notifies any mounted CommunicationTaskBoard.
    /// clientId may be null when the seller_id in the push payload could not be matched to a marketplace client.
    /// </summary>
    public void RequestChatOpen(string chatId, Guid? clientId)
    {
        _pendingChatId = chatId;
        _pendingChatClientId = clientId;
        PendingChatOpenChanged?.Invoke();
    }

    /// <summary>
    /// Returns and atomically clears the pending chat-open request.
    /// Returns null if no request is pending.
    /// ClientId may be null when the marketplace client could not be resolved from seller_id.
    /// </summary>
    public (string ChatId, Guid? ClientId)? TakePendingChatOpen()
    {
        if (_pendingChatId is null) return null;
        var result = (_pendingChatId, _pendingChatClientId);
        _pendingChatId = null;
        _pendingChatClientId = null;
        return result;
    }

    public void ClearActivePreview()
    {
        ActivePreviewExternalId = null;
        ActivePreviewClientId = null;
        ActivePreviewType = null;
    }
}


namespace StudioB2B.Infrastructure.Interfaces;

/// <summary>
/// Syncs open chats, questions, and reviews from Ozon into CommunicationTask records.
/// </summary>
public interface ICommunicationTaskSyncService
{
    /// <summary>Full sync — paginate through all data (used by background Hangfire job).</summary>
    Task<int> SyncAsync(CancellationToken ct = default);

    /// <summary>
    /// Fast lightweight sync — loads only one page of unread chats,
    /// unanswered questions and unprocessed reviews. Designed for on-page-open usage.
    /// </summary>
    Task<int> SyncRecentAsync(CancellationToken ct = default);
}

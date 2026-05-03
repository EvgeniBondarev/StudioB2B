namespace StudioB2B.Infrastructure.Interfaces;

/// <summary>
/// Syncs open chats, questions, and reviews from Ozon into CommunicationTask records.
/// </summary>
public interface ICommunicationTaskSyncService
{
    /// <summary>Full sync — all clients, upsert all fetched items.</summary>
    Task<int> SyncAsync(CancellationToken ct = default);

    /// <summary>Fast lightweight sync — one page per client.</summary>
    Task<int> SyncRecentAsync(CancellationToken ct = default);

    /// <summary>Upserts a single chat task triggered by a push notification.</summary>
    Task UpsertChatAsync(string chatId, string messageType, CancellationToken ct = default);
}

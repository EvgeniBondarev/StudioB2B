using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudioB2B.Domain.Constants;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Infrastructure.Persistence.Tenant;
using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Services.Communication;

public class CommunicationTaskSyncService : ICommunicationTaskSyncService
{
    private readonly TenantDbContext _db;
    private readonly IOzonChatService _chatService;
    private readonly IOzonQuestionsService _questionsService;
    private readonly IOzonReviewsService _reviewsService;
    private readonly ILogger<CommunicationTaskSyncService> _logger;

    public CommunicationTaskSyncService(
        TenantDbContext db,
        IOzonChatService chatService,
        IOzonQuestionsService questionsService,
        IOzonReviewsService reviewsService,
        ILogger<CommunicationTaskSyncService> logger)
    {
        _db = db;
        _chatService = chatService;
        _questionsService = questionsService;
        _reviewsService = reviewsService;
        _logger = logger;
    }

    public async Task SyncAsync(CancellationToken ct = default)
    {
        _db.SuppressAudit = true;
        try
        {
            await SyncChatsAsync(fullSync: true, ct);
            await SyncQuestionsAsync(fullSync: true, ct);
            await SyncReviewsAsync(fullSync: true, ct);
        }
        finally
        {
            _db.ChangeTracker.Clear();
            _db.SuppressAudit = false;
        }
    }

    public async Task SyncRecentAsync(CancellationToken ct = default)
    {
        _db.SuppressAudit = true;
        try
        {
            await SyncChatsAsync(fullSync: false, ct);
            await SyncQuestionsAsync(fullSync: false, ct);
            await SyncReviewsAsync(fullSync: false, ct);
        }
        finally
        {
            _db.ChangeTracker.Clear();
            _db.SuppressAudit = false;
        }
    }

    private async Task SyncChatsAsync(bool fullSync, CancellationToken ct)
    {
        try
        {
            List<OzonChatViewModelDto> chats;

            const string BuyerSellerType = "BUYER_SELLER";

            if (fullSync)
            {
                chats = await _chatService.GetAllChatsAsync(chatType: BuyerSellerType, ct: ct);
            }
            else
            {
                var page = await _chatService.GetChatsPageAsync(
                    pageSize: 50, chatStatus: "OPENED", chatType: BuyerSellerType, ct: ct);
                chats = page.Chats;
            }

            if (chats.Count == 0) return;

            var externalIds = chats.Select(c => c.ChatId).ToList();
            var existing = await _db.CommunicationTasks
                .Where(t => t.TaskType == CommunicationTaskType.Chat && externalIds.Contains(t.ExternalId))
                .ToDictionaryAsync(t => t.ExternalId, ct);

            if (existing.Count == 0) return;

            foreach (var chat in chats)
            {
                if (!existing.TryGetValue(chat.ChatId, out var task)) continue;

                task.ExternalStatus = chat.ChatStatus;
                task.ChatType ??= chat.ChatType;
                task.UnreadCount = chat.UnreadCount;
                task.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Synced {Count} existing chat tasks (full={Full})", existing.Count, fullSync);
        }
        catch (Exception ex)
        {
            _db.ChangeTracker.Clear();
            _logger.LogError(ex, "Failed to sync chats to task board");
        }
    }

    private async Task SyncQuestionsAsync(bool fullSync, CancellationToken ct)
    {
        try
        {
            var allQuestions = new List<OzonQuestionViewModelDto>();
            string? cursor = null;
            var maxPages = fullSync ? 10 : 1;
            var pageSize = fullSync ? 100 : 50;

            for (var i = 0; i < maxPages; i++)
            {
                var page = await _questionsService.GetQuestionsPageAsync(
                    pageSize: pageSize, cursor: cursor, ct: ct);

                allQuestions.AddRange(page.Questions);

                if (string.IsNullOrEmpty(page.NextCursor)) break;
                cursor = page.NextCursor;
            }

            if (allQuestions.Count == 0) return;

            var externalIds = allQuestions.Select(q => q.Id).ToList();
            var existing = await _db.CommunicationTasks
                .Where(t => t.TaskType == CommunicationTaskType.Question && externalIds.Contains(t.ExternalId))
                .ToDictionaryAsync(t => t.ExternalId, ct);

            if (existing.Count == 0) return;

            foreach (var q in allQuestions)
            {
                if (!existing.TryGetValue(q.Id, out var task)) continue;

                task.ExternalStatus = q.Status;
                task.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Synced {Count} existing question tasks (full={Full})", existing.Count, fullSync);
        }
        catch (Exception ex)
        {
            _db.ChangeTracker.Clear();
            _logger.LogError(ex, "Failed to sync questions to task board");
        }
    }

    private async Task SyncReviewsAsync(bool fullSync, CancellationToken ct)
    {
        try
        {
            var allReviews = new List<OzonReviewViewModelDto>();
            string? cursor = null;
            var maxPages = fullSync ? 10 : 1;
            var pageSize = fullSync ? 100 : 50;

            for (var i = 0; i < maxPages; i++)
            {
                var page = await _reviewsService.GetReviewsPageAsync(
                    pageSize: pageSize, cursor: cursor, ct: ct);

                allReviews.AddRange(page.Reviews);

                if (!page.HasNext) break;
                cursor = page.NextCursor;
            }

            if (allReviews.Count == 0) return;

            var externalIds = allReviews.Select(r => r.Id).ToList();
            var existing = await _db.CommunicationTasks
                .Where(t => t.TaskType == CommunicationTaskType.Review && externalIds.Contains(t.ExternalId))
                .ToDictionaryAsync(t => t.ExternalId, ct);

            if (existing.Count == 0) return;

            foreach (var r in allReviews)
            {
                if (!existing.TryGetValue(r.Id, out var task)) continue;

                task.ExternalStatus = r.Status;
                task.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Synced {Count} existing review tasks (full={Full})", existing.Count, fullSync);
        }
        catch (Exception ex)
        {
            _db.ChangeTracker.Clear();
            _logger.LogError(ex, "Failed to sync reviews to task board");
        }
    }

    private static bool IsTerminalChatStatus(string? status) =>
        string.Equals(status, "CLOSED", StringComparison.OrdinalIgnoreCase);

    private static bool IsTerminalQuestionStatus(string? status) =>
        string.Equals(status, "PROCESSED", StringComparison.OrdinalIgnoreCase);

    private static bool IsTerminalReviewStatus(string? status) =>
        string.Equals(status, "PROCESSED", StringComparison.OrdinalIgnoreCase);
}

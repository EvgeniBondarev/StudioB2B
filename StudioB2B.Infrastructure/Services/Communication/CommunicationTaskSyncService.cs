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
                    pageSize: 30, unreadOnly: true, chatType: BuyerSellerType, ct: ct);
                chats = page.Chats;

                if (chats.Count == 0)
                {
                    var openPage = await _chatService.GetChatsPageAsync(
                        pageSize: 30, chatStatus: "OPENED", chatType: BuyerSellerType, ct: ct);
                    chats = openPage.Chats;
                }
            }

            if (chats.Count == 0) return;

            var externalIds = chats.Select(c => c.ChatId).ToList();
            var existing = await _db.CommunicationTasks
                .Where(t => t.TaskType == CommunicationTaskType.Chat && externalIds.Contains(t.ExternalId))
                .ToDictionaryAsync(t => t.ExternalId, ct);

            foreach (var chat in chats)
            {
                if (existing.TryGetValue(chat.ChatId, out var task))
                {
                    task.ExternalStatus = chat.ChatStatus;
                    task.ChatType = chat.ChatType;
                    task.UnreadCount = chat.UnreadCount;
                    task.UpdatedAt = DateTime.UtcNow;
                    task.Title = $"Чат — {chat.MarketplaceClientName}";

                    if (IsTerminalChatStatus(chat.ChatStatus) &&
                        task.Status == CommunicationTaskStatus.New)
                    {
                        task.Status = CommunicationTaskStatus.Done;
                        task.CompletedAt = DateTime.UtcNow;
                    }
                }
                else
                {
                    var newTask = new CommunicationTask
                    {
                        Id = Guid.NewGuid(),
                        TaskType = CommunicationTaskType.Chat,
                        ExternalId = chat.ChatId,
                        MarketplaceClientId = chat.MarketplaceClientId,
                        Status = IsTerminalChatStatus(chat.ChatStatus)
                            ? CommunicationTaskStatus.Done
                            : CommunicationTaskStatus.New,
                        Title = $"Чат — {chat.MarketplaceClientName}",
                        PreviewText = null,
                        ExternalStatus = chat.ChatStatus,
                        ChatType = chat.ChatType,
                        UnreadCount = chat.UnreadCount,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        CompletedAt = IsTerminalChatStatus(chat.ChatStatus) ? DateTime.UtcNow : null
                    };

                    newTask.Logs.Add(new CommunicationTaskLog
                    {
                        Id = Guid.NewGuid(),
                        Action = "Created",
                        Details = $"Auto-created from Ozon chat {chat.ChatId}",
                        CreatedAt = DateTime.UtcNow
                    });

                    _db.CommunicationTasks.Add(newTask);
                }
            }

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Synced {Count} chats to task board (full={Full})", chats.Count, fullSync);
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
            var pageSize = fullSync ? 100 : 30;

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

            foreach (var q in allQuestions)
            {
                if (existing.TryGetValue(q.Id, out var task))
                {
                    task.ExternalStatus = q.Status;
                    task.UpdatedAt = DateTime.UtcNow;

                    if (IsTerminalQuestionStatus(q.Status) &&
                        task.Status == CommunicationTaskStatus.New)
                    {
                        task.Status = CommunicationTaskStatus.Done;
                        task.CompletedAt = DateTime.UtcNow;
                    }
                }
                else
                {
                    var newTask = new CommunicationTask
                    {
                        Id = Guid.NewGuid(),
                        TaskType = CommunicationTaskType.Question,
                        ExternalId = q.Id,
                        MarketplaceClientId = q.MarketplaceClientId,
                        Status = IsTerminalQuestionStatus(q.Status)
                            ? CommunicationTaskStatus.Done
                            : CommunicationTaskStatus.New,
                        Title = $"Вопрос — {q.MarketplaceClientName}",
                        PreviewText = q.Text.Length > 200 ? q.Text[..200] + "..." : q.Text,
                        ExternalStatus = q.Status,
                        ExternalUrl = q.QuestionLink,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        CompletedAt = IsTerminalQuestionStatus(q.Status) ? DateTime.UtcNow : null
                    };

                    newTask.Logs.Add(new CommunicationTaskLog
                    {
                        Id = Guid.NewGuid(),
                        Action = "Created",
                        Details = $"Auto-created from Ozon question {q.Id}",
                        CreatedAt = DateTime.UtcNow
                    });

                    _db.CommunicationTasks.Add(newTask);
                }
            }

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Synced {Count} questions to task board (full={Full})", allQuestions.Count, fullSync);
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
            var pageSize = fullSync ? 100 : 30;

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

            foreach (var r in allReviews)
            {
                if (existing.TryGetValue(r.Id, out var task))
                {
                    task.ExternalStatus = r.Status;
                    task.UpdatedAt = DateTime.UtcNow;

                    if (IsTerminalReviewStatus(r.Status) &&
                        task.Status == CommunicationTaskStatus.New)
                    {
                        task.Status = CommunicationTaskStatus.Done;
                        task.CompletedAt = DateTime.UtcNow;
                    }
                }
                else
                {
                    var newTask = new CommunicationTask
                    {
                        Id = Guid.NewGuid(),
                        TaskType = CommunicationTaskType.Review,
                        ExternalId = r.Id,
                        MarketplaceClientId = r.MarketplaceClientId,
                        Status = IsTerminalReviewStatus(r.Status)
                            ? CommunicationTaskStatus.Done
                            : CommunicationTaskStatus.New,
                        Title = $"Отзыв ({r.Rating}★) — {r.MarketplaceClientName}",
                        PreviewText = r.Text.Length > 200 ? r.Text[..200] + "..." : r.Text,
                        ExternalStatus = r.Status,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        CompletedAt = IsTerminalReviewStatus(r.Status) ? DateTime.UtcNow : null
                    };

                    newTask.Logs.Add(new CommunicationTaskLog
                    {
                        Id = Guid.NewGuid(),
                        Action = "Created",
                        Details = $"Auto-created from Ozon review {r.Id}",
                        CreatedAt = DateTime.UtcNow
                    });

                    _db.CommunicationTasks.Add(newTask);
                }
            }

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Synced {Count} reviews to task board (full={Full})", allReviews.Count, fullSync);
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

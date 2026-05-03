using System.Collections.Concurrent;
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
    private readonly ITenantDbContextFactory _dbContextFactory;
    private readonly IOzonChatService _chatService;
    private readonly IOzonQuestionsService _questionsService;
    private readonly IOzonReviewsService _reviewsService;
    private readonly ILogger<CommunicationTaskSyncService> _logger;
    private readonly ITaskBoardNotificationSender _notificationSender;
    private readonly ITenantProvider _tenantProvider;

    public CommunicationTaskSyncService(
        ITenantDbContextFactory dbContextFactory,
        IOzonChatService chatService,
        IOzonQuestionsService questionsService,
        IOzonReviewsService reviewsService,
        ILogger<CommunicationTaskSyncService> logger,
        ITaskBoardNotificationSender notificationSender,
        ITenantProvider tenantProvider)
    {
        _dbContextFactory = dbContextFactory;
        _chatService = chatService;
        _questionsService = questionsService;
        _reviewsService = reviewsService;
        _logger = logger;
        _notificationSender = notificationSender;
        _tenantProvider = tenantProvider;
    }

    public async Task<int> SyncAsync(CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        db.SuppressAudit = true;
        try
        {
            var r1 = await SyncChatsAsync(db, fullSync: true, ct);
            var r2 = await SyncQuestionsAsync(db, fullSync: true, ct);
            var r3 = await SyncReviewsAsync(db, fullSync: true, ct);
            var total = r1 + r2 + r3;
            if (total > 0 && _tenantProvider.TenantId.HasValue)
                await _notificationSender.SendBoardUpdatedAsync(_tenantProvider.TenantId.Value, ct);
            return total;
        }
        finally
        {
            db.SuppressAudit = false;
        }
    }

    public async Task<int> SyncRecentAsync(CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        db.SuppressAudit = true;
        try
        {
            var r1 = await SyncChatsAsync(db, fullSync: false, ct);
            var r2 = await SyncQuestionsAsync(db, fullSync: false, ct);
            var r3 = await SyncReviewsAsync(db, fullSync: false, ct);
            var total = r1 + r2 + r3;
            if (total > 0 && _tenantProvider.TenantId.HasValue)
                await _notificationSender.SendBoardUpdatedAsync(_tenantProvider.TenantId.Value, ct);
            return total;
        }
        finally
        {
            db.SuppressAudit = false;
        }
    }

    public async Task UpsertChatAsync(string chatId, string messageType, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        db.SuppressAudit = true;

        var existing = await db.CommunicationTasks
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TaskType == CommunicationTaskType.Chat &&
                                       t.ExternalId == chatId, ct);
        var now = DateTime.UtcNow;

        if (messageType == OzonPushMessageType.ChatClosed)
        {
            if (existing is not null)
            {
                existing.ExternalStatus = "CLOSED";
                existing.UpdatedAt = now;
                await db.SaveChangesAsync(ct);
                if (_tenantProvider.TenantId.HasValue)
                    await _notificationSender.SendBoardUpdatedAsync(_tenantProvider.TenantId.Value, ct);
                _logger.LogInformation("UpsertChatAsync: marked chat {ChatId} as CLOSED", chatId);
            }
            return;
        }

        if (existing is not null)
        {
            if (existing.IsDeleted)
            {
                existing.IsDeleted = false;
                existing.Status = CommunicationTaskStatus.New;
                existing.AssignedToUserId = null;
                existing.AssignedAt = null;
            }
            else if (existing.Status == CommunicationTaskStatus.Done)
            {
                existing.Status = CommunicationTaskStatus.New;
                existing.AssignedToUserId = null;
                existing.AssignedAt = null;
                db.CommunicationTaskLogs.Add(new CommunicationTaskLog
                {
                    Id = Guid.NewGuid(),
                    TaskId = existing.Id,
                    Action = "AutoReopened",
                    Details = "Новое сообщение (push notification)",
                    CreatedAt = now
                });
            }
            existing.UpdatedAt = now;
            await db.SaveChangesAsync(ct);
            if (_tenantProvider.TenantId.HasValue)
                await _notificationSender.SendBoardUpdatedAsync(_tenantProvider.TenantId.Value, ct);
            return;
        }

        // Not in DB yet — find which client owns this chat
        var clients = await GetAllClientsAsync(ct);
        OzonChatViewModelDto? found = null;

        foreach (var client in clients)
        {
            try
            {
                var page = await _chatService.GetChatsPageAsync(
                    pageSize: 100, cursor: null, chatStatus: null, chatType: null,
                    unreadOnly: false, marketplaceClientId: client.Id,
                    withLastMessageInfo: false, ct: ct);
                var match = page.Chats.FirstOrDefault(c => c.ChatId == chatId);
                if (match is not null) { found = match; break; }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "UpsertChatAsync: failed to search client {Name}", client.Name);
            }
        }

        if (found is null)
        {
            _logger.LogWarning("UpsertChatAsync: chat {ChatId} not found in any client's chat list", chatId);
            return;
        }

        var preview = await FetchChatPreviewAsync(found.MarketplaceClientId, chatId, ct);
        var rawTitle = $"Чат {found.MarketplaceClientName}";
        var title = rawTitle.Length > 500 ? rawTitle[..497] + "..." : rawTitle;

        db.CommunicationTasks.Add(new CommunicationTask
        {
            Id = Guid.NewGuid(),
            TaskType = CommunicationTaskType.Chat,
            ExternalId = chatId,
            MarketplaceClientId = found.MarketplaceClientId,
            Status = CommunicationTaskStatus.New,
            Title = title,
            ExternalStatus = found.ChatStatus,
            ChatType = found.ChatType,
            PreviewText = preview,
            CreatedAt = found.CreatedAt,
            UpdatedAt = now
        });

        await db.SaveChangesAsync(ct);
        if (_tenantProvider.TenantId.HasValue)
            await _notificationSender.SendBoardUpdatedAsync(_tenantProvider.TenantId.Value, ct);
        _logger.LogInformation("UpsertChatAsync: created new task for chat {ChatId}", chatId);
    }

    private async Task<int> SyncChatsAsync(TenantDbContext db, bool fullSync, CancellationToken ct)
    {
        try
        {
            var clients = await GetAllClientsAsync(ct);
            var allChats = new List<OzonChatViewModelDto>();

            foreach (var client in clients)
            {
                try
                {
                    var page = await _chatService.GetChatsPageAsync(
                        pageSize: 50, cursor: null, chatStatus: null,
                        chatType: "BUYER_SELLER", unreadOnly: false,
                        marketplaceClientId: client.Id, withLastMessageInfo: false, ct: ct);
                    allChats.AddRange(page.Chats);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "TaskBoardSync: failed to fetch chats for client {Name}", client.Name);
                }
            }

            if (allChats.Count == 0) return 0;

            var chatIds = allChats.Select(c => c.ChatId).ToList();
            var existing = await db.CommunicationTasks
                .IgnoreQueryFilters()
                .Where(t => t.TaskType == CommunicationTaskType.Chat && chatIds.Contains(t.ExternalId))
                .ToDictionaryAsync(t => t.ExternalId, ct);

            // Phase 1: fetch previews in parallel
            var previewMap = new ConcurrentDictionary<string, string?>();
            var sem = new SemaphoreSlim(4);

            async Task FetchChatPreviews(OzonChatViewModelDto chat)
            {
                await sem.WaitAsync(ct);
                try
                {
                    previewMap[chat.ChatId] = await FetchChatPreviewAsync(
                        chat.MarketplaceClientId, chat.ChatId, ct);
                }
                finally { sem.Release(); }
            }

            await Task.WhenAll(allChats.Select(FetchChatPreviews));

            // Phase 2: sequential upsert
            var now = DateTime.UtcNow;
            var changed = 0;

            foreach (var chat in allChats)
            {
                var preview = previewMap.GetValueOrDefault(chat.ChatId);

                if (existing.TryGetValue(chat.ChatId, out var task))
                {
                    // Restore soft-deleted record instead of failing on unique-key insert
                    if (task.IsDeleted)
                    {
                        task.IsDeleted = false;
                        task.Status = CommunicationTaskStatus.New;
                        task.AssignedToUserId = null;
                        task.AssignedAt = null;
                        changed++;
                    }

                    var prevStatus = task.ExternalStatus;
                    task.ExternalStatus = chat.ChatStatus;
                    task.ChatType ??= chat.ChatType;
                    task.UnreadCount = chat.UnreadCount;
                    task.UpdatedAt = now;
                    task.PreviewText = preview;

                    if (task.Status == CommunicationTaskStatus.Done &&
                        IsCustomerUserType(chat.LastMessageUserType))
                    {
                        task.Status = CommunicationTaskStatus.New;
                        task.AssignedToUserId = null;
                        task.AssignedAt = null;
                        db.CommunicationTaskLogs.Add(new CommunicationTaskLog
                        {
                            Id = Guid.NewGuid(),
                            TaskId = task.Id,
                            UserId = null,
                            Action = "AutoReopened",
                            Details = "Новое сообщение от покупателя после завершения",
                            CreatedAt = now
                        });
                        changed++;
                    }
                    else if (prevStatus != task.ExternalStatus)
                    {
                        changed++;
                    }
                }
                else if (!IsTerminalChatStatus(chat.ChatStatus))
                {
                    var rawTitle = $"Чат {chat.MarketplaceClientName}";
                    var title = rawTitle.Length > 500 ? rawTitle[..497] + "..." : rawTitle;
                    db.CommunicationTasks.Add(new CommunicationTask
                    {
                        Id = Guid.NewGuid(),
                        TaskType = CommunicationTaskType.Chat,
                        ExternalId = chat.ChatId,
                        MarketplaceClientId = chat.MarketplaceClientId,
                        Status = CommunicationTaskStatus.New,
                        Title = title,
                        ExternalStatus = chat.ChatStatus,
                        ChatType = chat.ChatType,
                        PreviewText = preview,
                        CreatedAt = chat.CreatedAt,
                        UpdatedAt = now
                    });
                    changed++;
                }
            }

            await db.SaveChangesAsync(ct);
            _logger.LogInformation("TaskBoardSync: processed {Total} chats, changed: {Changed}", allChats.Count, changed);

            var deepReopened = await AutoReopenDoneChatsAsync(db, ct);
            return changed + deepReopened;
        }
        catch (Exception ex)
        {
            db.ChangeTracker.Clear();
            _logger.LogError(ex, "Failed to sync chats to task board");
            return 0;
        }
    }

    private async Task<int> AutoReopenDoneChatsAsync(TenantDbContext db, CancellationToken ct)
    {
        try
        {
            var doneTasks = await db.CommunicationTasks
                .Where(t => t.TaskType == CommunicationTaskType.Chat &&
                            t.Status == CommunicationTaskStatus.Done &&
                            !t.IsDeleted)
                .ToListAsync(ct);

            if (doneTasks.Count == 0) return 0;

            var sem = new SemaphoreSlim(4);
            var toReopen = new ConcurrentBag<CommunicationTask>();

            async Task CheckOne(CommunicationTask task)
            {
                await sem.WaitAsync(ct);
                try
                {
                    var history = await _chatService.GetChatHistoryAsync(
                        task.MarketplaceClientId, task.ExternalId, "Backward", null, 1, ct);
                    var last = history?.Messages.FirstOrDefault();
                    if (last is null) return;
                    if (!IsCustomerUserType(last.User?.Type)) return;
                    toReopen.Add(task);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "AutoReopen check failed for task {Id}", task.Id);
                }
                finally
                {
                    sem.Release();
                }
            }

            await Task.WhenAll(doneTasks.Select(CheckOne));

            var reopened = 0;
            foreach (var task in toReopen)
            {
                task.Status = CommunicationTaskStatus.New;
                task.AssignedToUserId = null;
                task.AssignedAt = null;
                task.UpdatedAt = DateTime.UtcNow;

                db.CommunicationTaskLogs.Add(new CommunicationTaskLog
                {
                    Id = Guid.NewGuid(),
                    TaskId = task.Id,
                    UserId = null,
                    Action = "AutoReopened",
                    Details = "Новое сообщение от покупателя после завершения",
                    CreatedAt = DateTime.UtcNow
                });

                reopened++;
            }

            if (reopened > 0)
            {
                await db.SaveChangesAsync(ct);
                _logger.LogWarning("Auto-reopened {Count} done chat tasks with new buyer messages", reopened);
            }
            return reopened;
        }
        catch (Exception ex)
        {
            db.ChangeTracker.Clear();
            _logger.LogError(ex, "Failed to auto-reopen done chat tasks");
            return 0;
        }
    }

    private async Task<int> SyncQuestionsAsync(TenantDbContext db, bool fullSync, CancellationToken ct)
    {
        try
        {
            var clients = await GetAllClientsAsync(ct);
            var allQuestions = new List<OzonQuestionViewModelDto>();

            foreach (var client in clients)
            {
                try
                {
                    var page = await _questionsService.GetQuestionsPageAsync(
                        pageSize: 50, cursor: null, dateFrom: null, dateTo: null,
                        status: null, marketplaceClientId: client.Id, ct: ct);
                    allQuestions.AddRange(page.Questions);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "TaskBoardSync: failed to fetch questions for client {Name}", client.Name);
                }
            }

            if (allQuestions.Count == 0) return 0;

            var questionIds = allQuestions.Select(q => q.Id).ToList();
            var existing = await db.CommunicationTasks
                .IgnoreQueryFilters()
                .Where(t => t.TaskType == CommunicationTaskType.Question && questionIds.Contains(t.ExternalId))
                .ToDictionaryAsync(t => t.ExternalId, ct);

            // Phase 1: parallel preview fetch
            var previewMap = new ConcurrentDictionary<string, string?>();
            var sem = new SemaphoreSlim(4);

            async Task FetchQuestionPreviews(OzonQuestionViewModelDto q)
            {
                await sem.WaitAsync(ct);
                try { previewMap[q.Id] = await FetchQuestionPreviewAsync(q, ct); }
                finally { sem.Release(); }
            }

            await Task.WhenAll(allQuestions.Select(FetchQuestionPreviews));

            // Phase 2: sequential upsert
            var now = DateTime.UtcNow;
            var changed = 0;

            foreach (var q in allQuestions)
            {
                var preview = previewMap.GetValueOrDefault(q.Id);

                if (existing.TryGetValue(q.Id, out var task))
                {
                    if (task.IsDeleted)
                    {
                        task.IsDeleted = false;
                        task.Status = CommunicationTaskStatus.New;
                        task.AssignedToUserId = null;
                        task.AssignedAt = null;
                        changed++;
                    }

                    var prevStatus = task.ExternalStatus;
                    task.ExternalStatus = q.Status;
                    task.UpdatedAt = now;
                    task.PreviewText = preview;
                    if (prevStatus != task.ExternalStatus) changed++;
                }
                else if (!IsTerminalQuestionStatus(q.Status))
                {
                    var rawTitle = string.IsNullOrWhiteSpace(q.Text)
                        ? $"Вопрос {q.MarketplaceClientName}"
                        : q.Text;
                    var title = rawTitle.Length > 500 ? rawTitle[..497] + "..." : rawTitle;
                    db.CommunicationTasks.Add(new CommunicationTask
                    {
                        Id = Guid.NewGuid(),
                        TaskType = CommunicationTaskType.Question,
                        ExternalId = q.Id,
                        MarketplaceClientId = q.MarketplaceClientId,
                        Status = CommunicationTaskStatus.New,
                        Title = title,
                        PreviewText = preview,
                        ExternalStatus = q.Status,
                        ExternalUrl = q.QuestionLink,
                        CreatedAt = q.PublishedAt,
                        UpdatedAt = now
                    });
                    changed++;
                }
            }

            await db.SaveChangesAsync(ct);
            _logger.LogInformation("TaskBoardSync: processed {Total} questions, changed: {Changed}", allQuestions.Count, changed);

            return changed + await AutoReopenByStatusChangeAsync(db, existing.Values, IsTerminalQuestionStatus, ct);
        }
        catch (Exception ex)
        {
            db.ChangeTracker.Clear();
            _logger.LogError(ex, "Failed to sync questions to task board");
            return 0;
        }
    }

    private async Task<int> SyncReviewsAsync(TenantDbContext db, bool fullSync, CancellationToken ct)
    {
        try
        {
            var clients = await GetAllClientsAsync(ct);
            var allReviews = new List<OzonReviewViewModelDto>();

            foreach (var client in clients)
            {
                try
                {
                    var page = await _reviewsService.GetReviewsPageAsync(
                        pageSize: 50, cursor: null, status: null,
                        marketplaceClientId: client.Id, ct: ct);
                    allReviews.AddRange(page.Reviews);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "TaskBoardSync: failed to fetch reviews for client {Name}", client.Name);
                }
            }

            if (allReviews.Count == 0) return 0;

            var reviewIds = allReviews.Select(r => r.Id).ToList();
            var existing = await db.CommunicationTasks
                .IgnoreQueryFilters()
                .Where(t => t.TaskType == CommunicationTaskType.Review && reviewIds.Contains(t.ExternalId))
                .ToDictionaryAsync(t => t.ExternalId, ct);

            // Phase 1: parallel preview fetch
            var previewMap = new ConcurrentDictionary<string, string?>();
            var sem = new SemaphoreSlim(4);

            async Task FetchReviewPreviews(OzonReviewViewModelDto r)
            {
                await sem.WaitAsync(ct);
                try { previewMap[r.Id] = await FetchReviewPreviewAsync(r, ct); }
                finally { sem.Release(); }
            }

            await Task.WhenAll(allReviews.Select(FetchReviewPreviews));

            // Phase 2: sequential upsert
            var now = DateTime.UtcNow;
            var changed = 0;

            foreach (var r in allReviews)
            {
                var preview = previewMap.GetValueOrDefault(r.Id);

                if (existing.TryGetValue(r.Id, out var task))
                {
                    if (task.IsDeleted)
                    {
                        task.IsDeleted = false;
                        task.Status = CommunicationTaskStatus.New;
                        task.AssignedToUserId = null;
                        task.AssignedAt = null;
                        changed++;
                    }

                    var prevStatus = task.ExternalStatus;
                    task.ExternalStatus = r.Status;
                    task.UpdatedAt = now;
                    task.PreviewText = preview;
                    if (prevStatus != task.ExternalStatus) changed++;
                }
                else if (!IsTerminalReviewStatus(r.Status))
                {
                    var ratingStr = r.Rating > 0 ? $"★{r.Rating} " : "";
                    var rawTitle = string.IsNullOrWhiteSpace(r.Text)
                        ? $"Отзыв {r.MarketplaceClientName}"
                        : $"{ratingStr}{r.Text}";
                    var title = rawTitle.Length > 500 ? rawTitle[..497] + "..." : rawTitle;
                    db.CommunicationTasks.Add(new CommunicationTask
                    {
                        Id = Guid.NewGuid(),
                        TaskType = CommunicationTaskType.Review,
                        ExternalId = r.Id,
                        MarketplaceClientId = r.MarketplaceClientId,
                        Status = CommunicationTaskStatus.New,
                        Title = title,
                        PreviewText = preview,
                        ExternalStatus = r.Status,
                        CreatedAt = r.PublishedAt,
                        UpdatedAt = now
                    });
                    changed++;
                }
            }

            await db.SaveChangesAsync(ct);
            _logger.LogInformation("TaskBoardSync: processed {Total} reviews, changed: {Changed}", allReviews.Count, changed);
            return changed;
        }
        catch (Exception ex)
        {
            db.ChangeTracker.Clear();
            _logger.LogError(ex, "Failed to sync reviews to task board");
            return 0;
        }
    }

    private async Task<int> AutoReopenByStatusChangeAsync(
        TenantDbContext db,
        ICollection<CommunicationTask> syncedTasks,
        Func<string?, bool> isTerminalStatus,
        CancellationToken ct)
    {
        var reopened = 0;
        foreach (var task in syncedTasks)
        {
            if (task.Status != CommunicationTaskStatus.Done) continue;
            if (string.IsNullOrEmpty(task.ExternalStatus)) continue;
            if (isTerminalStatus(task.ExternalStatus)) continue;

            task.Status = CommunicationTaskStatus.New;
            task.AssignedToUserId = null;
            task.AssignedAt = null;
            task.UpdatedAt = DateTime.UtcNow;

            db.CommunicationTaskLogs.Add(new CommunicationTaskLog
            {
                Id = Guid.NewGuid(),
                TaskId = task.Id,
                UserId = null,
                Action = "AutoReopened",
                Details = "Статус задачи на площадке изменился — требуется повторная обработка",
                CreatedAt = DateTime.UtcNow
            });

            reopened++;
        }

        if (reopened > 0)
        {
            await db.SaveChangesAsync(ct);
            _logger.LogWarning("Auto-reopened {Count} tasks based on external status change", reopened);
        }
        return reopened;
    }

    private async Task<List<OzonChatClientInfoDto>> GetAllClientsAsync(CancellationToken ct)
    {
        await using var db = _dbContextFactory.CreateDbContext();
        return await db.MarketplaceClients!
            .AsNoTracking()
            .Where(c => !c.IsDeleted)
            .Select(c => new OzonChatClientInfoDto { Id = c.Id, Name = c.Name, ApiId = c.ApiId, EncryptedApiKey = c.Key })
            .ToListAsync(ct);
    }

    private async Task<string?> FetchChatPreviewAsync(
        Guid clientId, string chatId, CancellationToken ct)
    {
        try
        {
            var history = await _chatService.GetChatHistoryAsync(clientId, chatId, "Backward", null, 5, ct);
            if (history is null || history.Messages.Count == 0) return null;

            foreach (var msg in history.Messages)
            {
                if (msg.IsImage) return "[Изображение]";
                var text = msg.Data.FirstOrDefault(d => !string.IsNullOrWhiteSpace(d));
                if (text is not null)
                    return text[..Math.Min(2000, text.Length)];
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "FetchChatPreview failed for chat {ChatId}", chatId);
            return null;
        }
    }

    private async Task<string?> FetchQuestionPreviewAsync(OzonQuestionViewModelDto q, CancellationToken ct)
    {
        if (q.AnswersCount > 0)
        {
            try
            {
                var detail = await _questionsService.GetQuestionDetailAsync(q, ct);
                var last = detail.Answers.LastOrDefault();
                if (!string.IsNullOrWhiteSpace(last?.Text))
                    return last.Text[..Math.Min(2000, last.Text.Length)];
            }
            catch { }
        }

        return string.IsNullOrWhiteSpace(q.Text) ? null : q.Text[..Math.Min(2000, q.Text.Length)];
    }

    private async Task<string?> FetchReviewPreviewAsync(OzonReviewViewModelDto r, CancellationToken ct)
    {
        if (r.CommentsAmount > 0)
        {
            try
            {
                var detail = await _reviewsService.GetReviewDetailAsync(r, ct);
                var last = detail.Comments.LastOrDefault();
                if (!string.IsNullOrWhiteSpace(last?.Text))
                    return last.Text[..Math.Min(2000, last.Text.Length)];
            }
            catch { }
        }

        return string.IsNullOrWhiteSpace(r.Text) ? null : r.Text[..Math.Min(2000, r.Text.Length)];
    }

    private static bool IsCustomerUserType(string? t)
    {
        if (string.IsNullOrEmpty(t)) return false;
        return t is not ("Seller" or "seller" or "Seller_Support" or "SELLER_SUPPORT" or "Support");
    }

    private static bool IsTerminalChatStatus(string? s) =>
        string.Equals(s, "CLOSED", StringComparison.OrdinalIgnoreCase);

    private static bool IsTerminalQuestionStatus(string? status) =>
        string.Equals(status, "PROCESSED", StringComparison.OrdinalIgnoreCase);

    private static bool IsTerminalReviewStatus(string? s) =>
        string.Equals(s, "PROCESSED", StringComparison.OrdinalIgnoreCase);
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudioB2B.Domain.Constants;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Infrastructure.Persistence.Tenant;

namespace StudioB2B.Infrastructure.Services.Communication;

public class CommunicationTaskSyncService : ICommunicationTaskSyncService
{
    private readonly ITenantDbContextFactory _dbContextFactory;
    private readonly IOzonChatService _chatService;
    private readonly IOzonQuestionsService _questionsService;
    private readonly IOzonReviewsService _reviewsService;
    private readonly ILogger<CommunicationTaskSyncService> _logger;

    public CommunicationTaskSyncService(
        ITenantDbContextFactory dbContextFactory,
        IOzonChatService chatService,
        IOzonQuestionsService questionsService,
        IOzonReviewsService reviewsService,
        ILogger<CommunicationTaskSyncService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _chatService = chatService;
        _questionsService = questionsService;
        _reviewsService = reviewsService;
        _logger = logger;
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
            return r1 + r2 + r3;
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
            return r1 + r2 + r3;
        }
        finally
        {
            db.SuppressAudit = false;
        }
    }

    private async Task<int> SyncChatsAsync(TenantDbContext db, bool fullSync, CancellationToken ct)
    {
        try
        {
            // Как первая страница «Чаты» с типом «Покупатель — Продавец».
            var page = await _chatService.GetChatsPageAsync(
                pageSize: 20,
                cursor: null,
                chatStatus: null,
                chatType: "BUYER_SELLER",
                unreadOnly: false,
                marketplaceClientId: null,
                withLastMessageInfo: true,
                ct: ct);
            var chats = page.Chats;

            if (chats.Count == 0) return 0;

            var externalIds = chats.Select(c => c.ChatId).ToList();
            var existing = await db.CommunicationTasks
                .Where(t => t.TaskType == CommunicationTaskType.Chat && externalIds.Contains(t.ExternalId))
                .ToDictionaryAsync(t => t.ExternalId, ct);

            if (existing.Count == 0) return 0;

            var inlineReopened = 0;
            foreach (var chat in chats)
            {
                if (!existing.TryGetValue(chat.ChatId, out var task)) continue;

                task.ExternalStatus = chat.ChatStatus;
                task.ChatType ??= chat.ChatType;
                task.UnreadCount = chat.UnreadCount;
                task.UpdatedAt = DateTime.UtcNow;

                // Last message info is already fetched (withLastMessageInfo: true) — reopen Done tasks
                // that have unread messages or whose last message is from a customer.
                if (task.Status == CommunicationTaskStatus.Done &&
                    (chat.UnreadCount > 0 || IsCustomerUserType(chat.LastMessageUserType)))
                {
                    task.Status = CommunicationTaskStatus.New;
                    task.WasPreviouslyCompleted = true;
                    task.AssignedToUserId = null;
                    task.AssignedAt = null;

                    db.CommunicationTaskLogs.Add(new Domain.Entities.CommunicationTaskLog
                    {
                        Id = Guid.NewGuid(),
                        TaskId = task.Id,
                        UserId = null,
                        Action = "AutoReopened",
                        Details = "Новое сообщение от покупателя после завершения",
                        CreatedAt = DateTime.UtcNow
                    });

                    inlineReopened++;
                }
            }

            await db.SaveChangesAsync(ct);
            _logger.LogWarning("Synced {Count} existing chat tasks (full={Full}), inline-reopened {Reopened}",
                existing.Count, fullSync, inlineReopened);

            var deepReopened = await AutoReopenDoneChatsAsync(db, ct);
            return inlineReopened + deepReopened;
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
            var toReopen = new System.Collections.Concurrent.ConcurrentBag<Domain.Entities.CommunicationTask>();

            async Task CheckOne(Domain.Entities.CommunicationTask task)
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
                task.WasPreviouslyCompleted = true;
                task.AssignedToUserId = null;
                task.AssignedAt = null;
                task.UpdatedAt = DateTime.UtcNow;

                db.CommunicationTaskLogs.Add(new Domain.Entities.CommunicationTaskLog
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
            // Как Questions.razor LoadPageAsync: первая страница (20), без статуса и без обхода курсора.
            var page = await _questionsService.GetQuestionsPageAsync(
                pageSize: 20,
                cursor: null,
                dateFrom: null,
                dateTo: null,
                status: null,
                marketplaceClientId: null,
                ct: ct);
            var allQuestions = page.Questions;

            if (allQuestions.Count == 0) return 0;

            var externalIds = allQuestions.Select(q => q.Id).ToList();
            var existing = await db.CommunicationTasks
                .Where(t => t.TaskType == CommunicationTaskType.Question && externalIds.Contains(t.ExternalId))
                .ToDictionaryAsync(t => t.ExternalId, ct);

            if (existing.Count == 0) return 0;

            foreach (var q in allQuestions)
            {
                if (!existing.TryGetValue(q.Id, out var task)) continue;

                task.ExternalStatus = q.Status;
                task.UpdatedAt = DateTime.UtcNow;
            }

            await db.SaveChangesAsync(ct);
            _logger.LogWarning("Synced {Count} existing question tasks (full={Full})", existing.Count, fullSync);

            return await AutoReopenByStatusChangeAsync(db, existing.Values, IsTerminalQuestionStatus, ct);
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
            // Как Reviews.razor LoadPageAsync: первая страница (20), статус по умолчанию.
            var page = await _reviewsService.GetReviewsPageAsync(
                pageSize: 20,
                cursor: null,
                status: null,
                marketplaceClientId: null,
                ct: ct);
            var allReviews = page.Reviews;

            if (allReviews.Count == 0) return 0;

            var externalIds = allReviews.Select(r => r.Id).ToList();
            var existing = await db.CommunicationTasks
                .Where(t => t.TaskType == CommunicationTaskType.Review && externalIds.Contains(t.ExternalId))
                .ToDictionaryAsync(t => t.ExternalId, ct);

            if (existing.Count == 0) return 0;

            foreach (var r in allReviews)
            {
                if (!existing.TryGetValue(r.Id, out var task)) continue;

                task.ExternalStatus = r.Status;
                task.UpdatedAt = DateTime.UtcNow;
            }

            await db.SaveChangesAsync(ct);
            _logger.LogWarning("Synced {Count} existing review tasks (full={Full})", existing.Count, fullSync);

            return await AutoReopenByStatusChangeAsync(db, existing.Values, IsTerminalReviewStatus, ct);
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
        ICollection<Domain.Entities.CommunicationTask> syncedTasks,
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
            task.WasPreviouslyCompleted = true;
            task.AssignedToUserId = null;
            task.AssignedAt = null;
            task.UpdatedAt = DateTime.UtcNow;

            db.CommunicationTaskLogs.Add(new Domain.Entities.CommunicationTaskLog
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

    private static bool IsCustomerUserType(string? t)
    {
        if (string.IsNullOrEmpty(t)) return false;
        // Anything that is not a seller/support type is treated as a customer message.
        return t is not ("Seller" or "seller" or "Seller_Support" or "SELLER_SUPPORT" or "Support");
    }

    private static bool IsTerminalChatStatus(string? status) =>
        string.Equals(status, "CLOSED", StringComparison.OrdinalIgnoreCase);

    private static bool IsTerminalQuestionStatus(string? status) =>
        string.Equals(status, "PROCESSED", StringComparison.OrdinalIgnoreCase);

    private static bool IsTerminalReviewStatus(string? status) =>
        string.Equals(status, "PROCESSED", StringComparison.OrdinalIgnoreCase);
}

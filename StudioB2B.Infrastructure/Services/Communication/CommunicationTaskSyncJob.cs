using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudioB2B.Domain.Constants;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Infrastructure.Persistence.Tenant;
using StudioB2B.Infrastructure.Services.Ozon;
using StudioB2B.Shared;


namespace StudioB2B.Infrastructure.Services.Communication;

/// <summary>
/// Hangfire job that syncs open chats, questions, and reviews from Ozon
/// into the CommunicationTasks table. Runs outside of HTTP scope.
/// </summary>
public class CommunicationTaskSyncJob
{
    private readonly IKeyEncryptionService _encryption;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ITaskBoardNotificationSender _notificationSender;

    public CommunicationTaskSyncJob(
        IKeyEncryptionService encryption,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory,
        ITaskBoardNotificationSender notificationSender)
    {
        _encryption = encryption;
        _httpClientFactory = httpClientFactory;
        _loggerFactory = loggerFactory;
        _notificationSender = notificationSender;
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task ExecuteAsync(Guid tenantId, string connectionString, CancellationToken ct = default)
    {
        var logger = _loggerFactory.CreateLogger<CommunicationTaskSyncJob>();

        await using var db = CreateDbContext(connectionString);
        db.SuppressAudit = true;

        var api = new Ozon.OzonApiClient(_httpClientFactory, _encryption,
            _loggerFactory.CreateLogger<Ozon.OzonApiClient>());

        var clients = await db.MarketplaceClients!
            .AsNoTracking()
            .Where(c => !c.IsDeleted)
            .Select(c => new OzonChatClientInfoDto
            {
                Id = c.Id,
                Name = c.Name,
                ApiId = c.ApiId,
                EncryptedApiKey = c.Key
            })
            .ToListAsync(ct);

        if (clients.Count == 0)
        {
            logger.LogDebug("TaskBoardSync: no marketplace clients for tenant {TenantId}", tenantId);
            return;
        }

        var changed = false;
        changed |= await SyncChatsAsync(db, api, clients, logger, ct);
        changed |= await SyncQuestionsAsync(db, api, clients, logger, ct);
        changed |= await SyncReviewsAsync(db, api, clients, logger, ct);

        if (changed)
        {
            await _notificationSender.SendBoardUpdatedAsync(tenantId, ct);
        }

        logger.LogInformation("TaskBoardSync: completed for tenant {TenantId}", tenantId);
    }

    /// <summary>
    /// Upserts a single chat task triggered by a push notification.
    /// </summary>
    [AutomaticRetry(Attempts = 0)]
    public async Task UpsertChatTaskAsync(Guid tenantId, string connectionString, string chatId, string messageType,
        CancellationToken ct = default)
    {
        var logger = _loggerFactory.CreateLogger<CommunicationTaskSyncJob>();
        await using var db = CreateDbContext(connectionString);
        db.SuppressAudit = true;

        var existing = await db.CommunicationTasks
            .FirstOrDefaultAsync(t => t.TaskType == CommunicationTaskType.Chat &&
                                       t.ExternalId == chatId && !t.IsDeleted, ct);

        var now = DateTime.UtcNow;

        if (messageType == OzonPushMessageType.ChatClosed)
        {
            if (existing is not null)
            {
                existing.ExternalStatus = "CLOSED";
                existing.UpdatedAt = now;
                await db.SaveChangesAsync(ct);
                await _notificationSender.SendBoardUpdatedAsync(tenantId, ct);
                logger.LogInformation("UpsertChatTask: marked chat {ChatId} as CLOSED", chatId);
            }
            return;
        }

        if (existing is not null)
        {
            if (existing.Status == CommunicationTaskStatus.Done)
            {
                existing.Status = CommunicationTaskStatus.New;
                existing.WasPreviouslyCompleted = true;
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
            await _notificationSender.SendBoardUpdatedAsync(tenantId, ct);
            return;
        }

        var api = new OzonApiClient(_httpClientFactory, _encryption,
            _loggerFactory.CreateLogger<OzonApiClient>());

        var clients = await db.MarketplaceClients!
            .AsNoTracking()
            .Where(c => !c.IsDeleted)
            .Select(c => new OzonChatClientInfoDto { Id = c.Id, Name = c.Name, ApiId = c.ApiId, EncryptedApiKey = c.Key })
            .ToListAsync(ct);

        OzonChatClientInfoDto? owner = null;
        OzonChatDto? chatInfo = null;

        foreach (var client in clients)
        {
            try
            {
                var req = new OzonChatListRequestDto { Limit = 100, Filter = new OzonChatListFilterDto() };
                var result = await api.GetChatListAsync(client.ApiId, client.EncryptedApiKey, req, ct);
                if (!result.IsSuccess || result.Data is null) continue;

                var found = result.Data.Chats.FirstOrDefault(c => c.Chat?.ChatId == chatId);
                if (found?.Chat is not null)
                {
                    owner = client;
                    chatInfo = found.Chat;
                    break;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "UpsertChatTask: failed to fetch chat list for client {Client}", client.Name);
            }
        }

        if (owner is null || chatInfo is null)
        {
            logger.LogWarning("UpsertChatTask: chat {ChatId} not found in any client's chat list", chatId);
            return;
        }

        db.CommunicationTasks.Add(new CommunicationTask
        {
            Id = Guid.NewGuid(),
            TaskType = CommunicationTaskType.Chat,
            ExternalId = chatId,
            MarketplaceClientId = owner.Id,
            Status = CommunicationTaskStatus.New,
            Title = $"Чат {owner.Name}",
            ExternalStatus = chatInfo.ChatStatus ?? "",
            ChatType = chatInfo.ChatType,
            CreatedAt = chatInfo.CreatedAt,
            UpdatedAt = now
        });

        await db.SaveChangesAsync(ct);
        await _notificationSender.SendBoardUpdatedAsync(tenantId, ct);
        logger.LogInformation("UpsertChatTask: created new task for chat {ChatId}", chatId);
    }

    private static async Task<bool> SyncChatsAsync(
        TenantDbContext db, OzonApiClient api,
        List<OzonChatClientInfoDto> clients,
        ILogger logger, CancellationToken ct)
    {
        var allChats = new List<(Guid ClientId, string ClientName, OzonChatDto Chat)>();

        foreach (var client in clients)
        {
            try
            {
                var request = new OzonChatListRequestDto
                {
                    Limit = 100,
                    Filter = new OzonChatListFilterDto()
                };

                var result = await api.GetChatListAsync(client.ApiId, client.EncryptedApiKey, request, ct);
                if (result.IsSuccess && result.Data?.Chats is not null)
                {
                    foreach (var item in result.Data.Chats)
                    {
                        if (item.Chat is not null)
                            allChats.Add((client.Id, client.Name, item.Chat));
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "TaskBoardSync: failed to fetch chats for client {Client}", client.Name);
            }
        }

        if (allChats.Count == 0) return false;

        var chatIds = allChats.Select(c => c.Chat.ChatId).ToList();
        var existing = await db.CommunicationTasks
            .Where(t => t.TaskType == CommunicationTaskType.Chat && chatIds.Contains(t.ExternalId))
            .ToDictionaryAsync(t => t.ExternalId, ct);

        var now = DateTime.UtcNow;
        var changed = false;

        foreach (var (clientId, clientName, chat) in allChats)
        {
            if (existing.TryGetValue(chat.ChatId, out var task))
            {
                var prevStatus = task.ExternalStatus;
                task.ExternalStatus = chat.ChatStatus ?? "";
                task.UpdatedAt = now;

                if (task.Status == CommunicationTaskStatus.Done && !IsTerminalChat(chat.ChatStatus ?? ""))
                {
                    task.Status = CommunicationTaskStatus.New;
                    task.WasPreviouslyCompleted = true;
                    task.AssignedToUserId = null;
                    task.AssignedAt = null;
                    db.CommunicationTaskLogs.Add(new CommunicationTaskLog
                    {
                        Id = Guid.NewGuid(),
                        TaskId = task.Id,
                        Action = "AutoReopened",
                        Details = "Chat reopened by sync",
                        CreatedAt = now
                    });
                }

                if (prevStatus != task.ExternalStatus) changed = true;
            }
            else if (!IsTerminalChat(chat.ChatStatus ?? ""))
            {
                db.CommunicationTasks.Add(new CommunicationTask
                {
                    Id = Guid.NewGuid(),
                    TaskType = CommunicationTaskType.Chat,
                    ExternalId = chat.ChatId,
                    MarketplaceClientId = clientId,
                    Status = CommunicationTaskStatus.New,
                    Title = $"Чат {clientName}",
                    ExternalStatus = chat.ChatStatus ?? "",
                    ChatType = chat.ChatType,
                    CreatedAt = chat.CreatedAt,
                    UpdatedAt = now
                });
                changed = true;
            }
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("TaskBoardSync: processed {Total} chats, existing: {Ex}, changed: {Changed}",
            allChats.Count, existing.Count, changed);
        return changed;
    }

    private static async Task<bool> SyncQuestionsAsync(
        TenantDbContext db, OzonApiClient api,
        List<OzonChatClientInfoDto> clients,
        ILogger logger, CancellationToken ct)
    {
        var allQuestions = new List<(Guid ClientId, string ClientName, OzonQuestionItemDto Question)>();

        foreach (var client in clients)
        {
            try
            {
                var result = await api.GetQuestionListAsync(client.ApiId, client.EncryptedApiKey,
                    new OzonQuestionListRequestDto(), ct);
                if (result.IsSuccess && result.Data?.Questions is not null)
                {
                    foreach (var q in result.Data.Questions)
                        allQuestions.Add((client.Id, client.Name, q));
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "TaskBoardSync: failed to fetch questions for client {Client}", client.Name);
            }
        }

        if (allQuestions.Count == 0) return false;

        var questionIds = allQuestions.Select(q => q.Question.Id).ToList();
        var existing = await db.CommunicationTasks
            .Where(t => t.TaskType == CommunicationTaskType.Question && questionIds.Contains(t.ExternalId))
            .ToDictionaryAsync(t => t.ExternalId, ct);

        var now = DateTime.UtcNow;
        var changed = false;

        foreach (var (clientId, clientName, q) in allQuestions)
        {
            if (existing.TryGetValue(q.Id, out var task))
            {
                var prevStatus = task.ExternalStatus;
                task.ExternalStatus = q.Status.ToString();
                task.UpdatedAt = now;

                if (task.Status == CommunicationTaskStatus.Done && !IsTerminalQuestion(q.Status.ToString()))
                {
                    task.Status = CommunicationTaskStatus.New;
                    task.WasPreviouslyCompleted = true;
                    task.AssignedToUserId = null;
                    task.AssignedAt = null;
                }

                if (prevStatus != task.ExternalStatus) changed = true;
            }
            else if (!IsTerminalQuestion(q.Status.ToString()))
            {
                var title = string.IsNullOrWhiteSpace(q.Text)
                    ? $"Вопрос {clientName}"
                    : q.Text.Length > 500 ? q.Text[..497] + "..." : q.Text;

                db.CommunicationTasks.Add(new CommunicationTask
                {
                    Id = Guid.NewGuid(),
                    TaskType = CommunicationTaskType.Question,
                    ExternalId = q.Id,
                    MarketplaceClientId = clientId,
                    Status = CommunicationTaskStatus.New,
                    Title = title,
                    PreviewText = q.Text.Length > 0 ? q.Text[..Math.Min(2000, q.Text.Length)] : null,
                    ExternalStatus = q.Status.ToString(),
                    ExternalUrl = q.QuestionLink,
                    CreatedAt = q.PublishedAt,
                    UpdatedAt = now
                });
                changed = true;
            }
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("TaskBoardSync: processed {Total} questions, changed: {Changed}",
            allQuestions.Count, changed);
        return changed;
    }

    private static async Task<bool> SyncReviewsAsync(
        TenantDbContext db, OzonApiClient api,
        List<OzonChatClientInfoDto> clients,
        ILogger logger, CancellationToken ct)
    {
        var allReviews = new List<(Guid ClientId, string ClientName, OzonReviewListItemDto Review)>();

        foreach (var client in clients)
        {
            try
            {
                var request = new OzonReviewListRequestDto
                {
                    Limit = 100,
                    SortDir = "DESC",
                    Status = "ALL"
                };

                var result = await api.GetReviewListAsync(client.ApiId, client.EncryptedApiKey, request, ct);
                if (result.IsSuccess && result.Data?.Reviews is not null)
                {
                    foreach (var r in result.Data.Reviews)
                        allReviews.Add((client.Id, client.Name, r));
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "TaskBoardSync: failed to fetch reviews for client {Client}", client.Name);
            }
        }

        if (allReviews.Count == 0) return false;

        var reviewIds = allReviews.Select(r => r.Review.Id).ToList();
        var existing = await db.CommunicationTasks
            .Where(t => t.TaskType == CommunicationTaskType.Review && reviewIds.Contains(t.ExternalId))
            .ToDictionaryAsync(t => t.ExternalId, ct);

        var now = DateTime.UtcNow;
        var changed = false;

        foreach (var (clientId, clientName, r) in allReviews)
        {
            if (existing.TryGetValue(r.Id, out var task))
            {
                var prevStatus = task.ExternalStatus;
                task.ExternalStatus = r.Status ?? "";
                task.UpdatedAt = now;

                if (task.Status == CommunicationTaskStatus.Done && !IsTerminalReview(r.Status ?? ""))
                {
                    task.Status = CommunicationTaskStatus.New;
                    task.WasPreviouslyCompleted = true;
                    task.AssignedToUserId = null;
                    task.AssignedAt = null;
                }

                if (prevStatus != task.ExternalStatus) changed = true;
            }
            else if (!IsTerminalReview(r.Status ?? ""))
            {
                var ratingStr = r.Rating > 0 ? $"★{r.Rating} " : "";
                var rawTitle = string.IsNullOrWhiteSpace(r.Text)
                    ? $"Отзыв {clientName}"
                    : $"{ratingStr}{r.Text}";
                var title = rawTitle.Length > 500 ? rawTitle[..497] + "..." : rawTitle;
                var preview = r.Text.Length > 0 ? r.Text[..Math.Min(2000, r.Text.Length)] : null;

                db.CommunicationTasks.Add(new CommunicationTask
                {
                    Id = Guid.NewGuid(),
                    TaskType = CommunicationTaskType.Review,
                    ExternalId = r.Id,
                    MarketplaceClientId = clientId,
                    Status = CommunicationTaskStatus.New,
                    Title = title,
                    PreviewText = preview,
                    ExternalStatus = r.Status ?? "",
                    CreatedAt = r.PublishedAt,
                    UpdatedAt = now
                });
                changed = true;
            }
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("TaskBoardSync: processed {Total} reviews, changed: {Changed}",
            allReviews.Count, changed);
        return changed;
    }

    private static TenantDbContext CreateDbContext(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
        optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        return new TenantDbContext(optionsBuilder.Options, currentUserProvider: null);
    }

    private static bool IsTerminalChat(string s) =>
        s.Equals("CLOSED", StringComparison.OrdinalIgnoreCase);

    private static bool IsTerminalQuestion(string s) =>
        s.Equals("PROCESSED", StringComparison.OrdinalIgnoreCase);

    private static bool IsTerminalReview(string s) =>
        s.Equals("PROCESSED", StringComparison.OrdinalIgnoreCase);
}

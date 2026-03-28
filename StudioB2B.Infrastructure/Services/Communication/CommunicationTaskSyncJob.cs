using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudioB2B.Domain.Constants;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Infrastructure.Persistence.Tenant;
using StudioB2B.Shared;
using StudioB2B.Shared.DTOs;


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

    private static async Task<bool> SyncChatsAsync(
        TenantDbContext db, IOzonApiClient api,
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

        var added = 0;
        foreach (var (clientId, clientName, chat) in allChats)
        {
            var externalId = chat.ChatId;
            var status = chat.ChatStatus ?? "";

            if (existing.TryGetValue(externalId, out var task))
            {
                task.ExternalStatus = status;
                task.UpdatedAt = DateTime.UtcNow;

                if (IsTerminalChat(status) && task.Status == CommunicationTaskStatus.New)
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
                    ExternalId = externalId,
                    MarketplaceClientId = clientId,
                    Status = IsTerminalChat(status)
                        ? CommunicationTaskStatus.Done
                        : CommunicationTaskStatus.New,
                    Title = $"Чат — {clientName}",
                    ExternalStatus = status,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CompletedAt = IsTerminalChat(status) ? DateTime.UtcNow : null
                };

                newTask.Logs.Add(new CommunicationTaskLog
                {
                    Id = Guid.NewGuid(),
                    Action = "Created",
                    Details = $"Auto-sync from Ozon chat {externalId}",
                    CreatedAt = DateTime.UtcNow
                });

                db.CommunicationTasks.Add(newTask);
                added++;
            }
        }

        if (added > 0) await db.SaveChangesAsync(ct);
        logger.LogInformation("TaskBoardSync: chats fetched={Total}, new={Added}", allChats.Count, added);
        return added > 0;
    }

    private static async Task<bool> SyncQuestionsAsync(
        TenantDbContext db, IOzonApiClient api,
        List<OzonChatClientInfoDto> clients,
        ILogger logger, CancellationToken ct)
    {
        var allQuestions = new List<(Guid ClientId, string ClientName, OzonQuestionItemDto Question)>();

        foreach (var client in clients)
        {
            try
            {
                var request = new OzonQuestionListRequestDto();

                var result = await api.GetQuestionListAsync(client.ApiId, client.EncryptedApiKey, request, ct);
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

        var added = 0;
        foreach (var (clientId, clientName, q) in allQuestions)
        {
            var externalId = q.Id;
            var status = q.Status.ToString();

            if (existing.TryGetValue(externalId, out var task))
            {
                task.ExternalStatus = status;
                task.UpdatedAt = DateTime.UtcNow;

                if (IsTerminalQuestion(status) && task.Status == CommunicationTaskStatus.New)
                {
                    task.Status = CommunicationTaskStatus.Done;
                    task.CompletedAt = DateTime.UtcNow;
                }
            }
            else
            {
                var preview = q.Text?.Length > 200 ? q.Text[..200] + "..." : q.Text;
                var newTask = new CommunicationTask
                {
                    Id = Guid.NewGuid(),
                    TaskType = CommunicationTaskType.Question,
                    ExternalId = externalId,
                    MarketplaceClientId = clientId,
                    Status = IsTerminalQuestion(status)
                        ? CommunicationTaskStatus.Done
                        : CommunicationTaskStatus.New,
                    Title = $"Вопрос — {clientName}",
                    PreviewText = preview,
                    ExternalStatus = status,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CompletedAt = IsTerminalQuestion(status) ? DateTime.UtcNow : null
                };

                newTask.Logs.Add(new CommunicationTaskLog
                {
                    Id = Guid.NewGuid(),
                    Action = "Created",
                    Details = $"Auto-sync from Ozon question {externalId}",
                    CreatedAt = DateTime.UtcNow
                });

                db.CommunicationTasks.Add(newTask);
                added++;
            }
        }

        if (added > 0) await db.SaveChangesAsync(ct);
        logger.LogInformation("TaskBoardSync: questions fetched={Total}, new={Added}", allQuestions.Count, added);
        return added > 0;
    }

    private static async Task<bool> SyncReviewsAsync(
        TenantDbContext db, IOzonApiClient api,
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

        var added = 0;
        foreach (var (clientId, clientName, r) in allReviews)
        {
            var externalId = r.Id;
            var status = r.Status ?? "";

            if (existing.TryGetValue(externalId, out var task))
            {
                task.ExternalStatus = status;
                task.UpdatedAt = DateTime.UtcNow;

                if (IsTerminalReview(status) && task.Status == CommunicationTaskStatus.New)
                {
                    task.Status = CommunicationTaskStatus.Done;
                    task.CompletedAt = DateTime.UtcNow;
                }
            }
            else
            {
                var preview = r.Text?.Length > 200 ? r.Text[..200] + "..." : r.Text;
                var newTask = new CommunicationTask
                {
                    Id = Guid.NewGuid(),
                    TaskType = CommunicationTaskType.Review,
                    ExternalId = externalId,
                    MarketplaceClientId = clientId,
                    Status = IsTerminalReview(status)
                        ? CommunicationTaskStatus.Done
                        : CommunicationTaskStatus.New,
                    Title = $"Отзыв ({r.Rating}★) — {clientName}",
                    PreviewText = preview,
                    ExternalStatus = status,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CompletedAt = IsTerminalReview(status) ? DateTime.UtcNow : null
                };

                newTask.Logs.Add(new CommunicationTaskLog
                {
                    Id = Guid.NewGuid(),
                    Action = "Created",
                    Details = $"Auto-sync from Ozon review {externalId}",
                    CreatedAt = DateTime.UtcNow
                });

                db.CommunicationTasks.Add(newTask);
                added++;
            }
        }

        if (added > 0) await db.SaveChangesAsync(ct);
        logger.LogInformation("TaskBoardSync: reviews fetched={Total}, new={Added}", allReviews.Count, added);
        return added > 0;
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

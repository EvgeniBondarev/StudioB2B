using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using StudioB2B.Domain.Constants;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Infrastructure.Persistence.Tenant;
using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Services.Communication;

public class CommunicationTaskService : ICommunicationTaskService
{
    private readonly ITenantDbContextFactory _dbContextFactory;
    private readonly ILogger<CommunicationTaskService> _logger;
    private readonly ICurrentUserProvider _currentUser;
    private readonly IOzonChatService _chatService;
    private readonly IOzonQuestionsService _questionsService;
    private readonly IOzonReviewsService _reviewsService;
    private readonly IMemoryCache _cache;
    private readonly ITenantProvider _tenantProvider;
    private readonly ITaskBoardNotificationSender _notificationSender;
    private readonly HashSet<string> _liveCacheKeys = new();

    public CommunicationTaskService(
        ITenantDbContextFactory dbContextFactory,
        ILogger<CommunicationTaskService> logger,
        ICurrentUserProvider currentUser,
        IOzonChatService chatService,
        IOzonQuestionsService questionsService,
        IOzonReviewsService reviewsService,
        IMemoryCache cache,
        ITenantProvider tenantProvider,
        ITaskBoardNotificationSender notificationSender)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
        _currentUser = currentUser;
        _chatService = chatService;
        _questionsService = questionsService;
        _reviewsService = reviewsService;
        _cache = cache;
        _tenantProvider = tenantProvider;
        _notificationSender = notificationSender;
    }

    public async Task<Guid?> GetCurrentUserTenantIdAsync(CancellationToken ct = default)
    {
        if (_currentUser.UserId is null) return null;
        using var db = _dbContextFactory.CreateDbContext();
        return await ResolveUserIdAsync(db, _currentUser.UserId.Value, ct);
    }

    public void InvalidateLiveCache()
    {
        foreach (var key in _liveCacheKeys)
            _cache.Remove(key);
        _liveCacheKeys.Clear();
    }

    /// <summary>
    /// Resolves <paramref name="userId"/> to an existing tenant Users.Id.
    /// If the exact ID exists — returns it. Otherwise looks up by email.
    /// If nothing found — creates a stub user record. Returns the usable FK-safe Id.
    /// </summary>
    private async Task<Guid> ResolveUserIdAsync(TenantDbContext db, Guid userId, CancellationToken ct)
    {
        if (await db.Users.AnyAsync(u => u.Id == userId, ct))
            return userId;

        var email = _currentUser.Email;
        if (email is not null)
        {
            var existingId = await db.Users
                .Where(u => u.Email == email)
                .Select(u => u.Id)
                .FirstOrDefaultAsync(ct);

            if (existingId != Guid.Empty)
                return existingId;
        }

        db.Users.Add(new TenantUser
        {
            Id = userId,
            Email = email ?? $"{userId}@stub",
            FirstName = email?.Split('@').FirstOrDefault() ?? "User",
            LastName = "",
            HashPassword = "",
            IsActive = true
        });
        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Created stub tenant user {UserId} for task board FK", userId);
        return userId;
    }

    public async Task<TaskBoardDto> GetBoardAsync(CommunicationTaskFilter filter, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();

        var dbQuery = db.CommunicationTasks
            .AsNoTracking()
            .Where(t => !t.IsDeleted &&
                        (t.TaskType != CommunicationTaskType.Chat ||
                         t.ChatType == null ||
                         t.ChatType == "BUYER_SELLER"));

        if (filter.TaskTypes.Count > 0)
            dbQuery = dbQuery.Where(t => filter.TaskTypes.Contains(t.TaskType));
        else if (filter.TaskType.HasValue)
            dbQuery = dbQuery.Where(t => t.TaskType == filter.TaskType.Value);
        if (filter.AssignedToUserIds.Count > 0)
            dbQuery = dbQuery.Where(t => t.AssignedToUserId != null && filter.AssignedToUserIds.Contains(t.AssignedToUserId.Value));
        else if (filter.AssignedToUserId.HasValue)
            dbQuery = dbQuery.Where(t => t.AssignedToUserId == filter.AssignedToUserId.Value);
        if (filter.MarketplaceClientIds.Count > 0)
            dbQuery = dbQuery.Where(t => filter.MarketplaceClientIds.Contains(t.MarketplaceClientId));
        else if (filter.MarketplaceClientId.HasValue)
            dbQuery = dbQuery.Where(t => t.MarketplaceClientId == filter.MarketplaceClientId.Value);

        var inProgressItems = await dbQuery
            .Where(t => t.Status == CommunicationTaskStatus.InProgress)
            .OrderByDescending(t => t.CreatedAt)
            .Select(ProjectToCardDto())
            .ToListAsync(ct);

        var doneQuery = dbQuery
            .Where(t => t.Status == CommunicationTaskStatus.Done ||
                        t.Status == CommunicationTaskStatus.Cancelled);

        if (filter.From.HasValue)
            doneQuery = doneQuery.Where(t => t.CreatedAt >= filter.From.Value);
        if (filter.To.HasValue)
            doneQuery = doneQuery.Where(t => t.CreatedAt <= filter.To.Value);

        var doneTotalCount = await doneQuery.CountAsync(ct);
        var doneItems = await doneQuery
            .OrderByDescending(t => t.CreatedAt)
            .Take(filter.DoneTake)
            .Select(ProjectToCardDto())
            .ToListAsync(ct);

        var newItems = new List<CommunicationTaskDto>();
        if (!filter.AssignedToUserId.HasValue)
        {
            var existingKeys = await LoadExistingTaskKeysForNewExclusionAsync(db, filter, ct);
            var noTypeFilter = !filter.TaskType.HasValue && filter.TaskTypes.Count == 0;
            var clientIds = filter.MarketplaceClientIds.Count > 0
                ? filter.MarketplaceClientIds
                : filter.MarketplaceClientId.HasValue ? new List<Guid> { filter.MarketplaceClientId.Value } : (List<Guid>?)null;

            if (noTypeFilter || filter.TaskTypes.Contains(CommunicationTaskType.Chat) || filter.TaskType == CommunicationTaskType.Chat)
            {
                if (clientIds is { Count: > 0 })
                    foreach (var cid in clientIds)
                        newItems.AddRange(await FetchLiveChatsAsync(cid, existingKeys, filter.From, filter.To, ct));
                else
                    newItems.AddRange(await FetchLiveChatsAsync(null, existingKeys, filter.From, filter.To, ct));
            }
            if (noTypeFilter || filter.TaskTypes.Contains(CommunicationTaskType.Question) || filter.TaskType == CommunicationTaskType.Question)
            {
                if (clientIds is { Count: > 0 })
                    foreach (var cid in clientIds)
                        newItems.AddRange(await FetchLiveQuestionsAsync(cid, existingKeys, filter.From, filter.To, ct));
                else
                    newItems.AddRange(await FetchLiveQuestionsAsync(null, existingKeys, filter.From, filter.To, ct));
            }
            if (noTypeFilter || filter.TaskTypes.Contains(CommunicationTaskType.Review) || filter.TaskType == CommunicationTaskType.Review)
            {
                if (clientIds is { Count: > 0 })
                    foreach (var cid in clientIds)
                        newItems.AddRange(await FetchLiveReviewsAsync(cid, existingKeys, filter.From, filter.To, ct));
                else
                    newItems.AddRange(await FetchLiveReviewsAsync(null, existingKeys, filter.From, filter.To, ct));
            }
        }

        await EnrichChatCardsFromHistoryAsync(newItems, ct);
        await EnrichChatCardsFromHistoryAsync(inProgressItems, ct);
        await EnrichChatCardsFromHistoryAsync(doneItems, ct);

        var doneTypeCounts = await doneQuery
            .GroupBy(t => t.TaskType)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var typeCounts = new Dictionary<CommunicationTaskType, int>();
        foreach (var tc in doneTypeCounts)
            typeCounts[tc.Key] = tc.Count;
        foreach (var t in inProgressItems)
            typeCounts[t.TaskType] = typeCounts.GetValueOrDefault(t.TaskType) + 1;
        foreach (var t in newItems)
            typeCounts[t.TaskType] = typeCounts.GetValueOrDefault(t.TaskType) + 1;

        var result = new TaskBoardDto
        {
            DoneTotalCount = doneTotalCount,
            TypeCounts = typeCounts
        };

        result.NewTasks.AddRange(newItems);
        result.InProgressTasks.AddRange(inProgressItems);
        result.DoneTasks.AddRange(doneItems);

        var activeRates = await db.CommunicationPaymentRates
            .Include(r => r.User)
            .AsNoTracking()
            .Where(r => r.IsActive)
            .ToListAsync(ct);

        var globalRates = activeRates.Where(r => r.UserId == null).ToList();

        foreach (var type in new[] { CommunicationTaskType.Chat, CommunicationTaskType.Question, CommunicationTaskType.Review })
        {
            var matching = globalRates.Where(r => r.TaskType == null || r.TaskType == type).ToList();

            if (matching.Any(r => r.TaskType == type))
                matching = matching.Where(r => r.TaskType == type).ToList();

            var perTask = matching
                .Where(r => r.PaymentMode == PaymentMode.PerTask)
                .Sum(r => r.Rate);
            var timedHourly = matching
                .Where(r => r.PaymentMode == PaymentMode.Hourly && r.MaxDurationMinutes.HasValue)
                .Sum(r => r.Rate);
            var fixedTotal = perTask + timedHourly;
            if (fixedTotal > 0) result.PaymentEstimates[type] = Math.Round(fixedTotal, 2);

            var hourly = matching
                .Where(r => r.PaymentMode == PaymentMode.Hourly && !r.MaxDurationMinutes.HasValue)
                .Sum(r => r.Rate);
            if (hourly > 0) result.HourlyEstimates[type] = Math.Round(hourly, 2);
        }

        result.ActiveRates.AddRange(activeRates.Select(r => new CommunicationPaymentRateDto
        {
            Id = r.Id,
            TaskType = r.TaskType,
            PaymentMode = r.PaymentMode,
            UserId = r.UserId,
            Rate = r.Rate,
            MinDurationMinutes = r.MinDurationMinutes,
            MaxDurationMinutes = r.MaxDurationMinutes,
            IsActive = r.IsActive,
            Description = r.Description
        }));

        return result;
    }


    public async Task<TaskBoardDto> GetDbBoardAsync(CommunicationTaskFilter filter, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        db.ChangeTracker.Clear();

        var dbQuery = db.CommunicationTasks
            .AsNoTracking()
            .Where(t => !t.IsDeleted &&
                        (t.TaskType != CommunicationTaskType.Chat ||
                         t.ChatType == null ||
                         t.ChatType == "BUYER_SELLER"));

        if (filter.TaskTypes.Count > 0)
            dbQuery = dbQuery.Where(t => filter.TaskTypes.Contains(t.TaskType));
        else if (filter.TaskType.HasValue)
            dbQuery = dbQuery.Where(t => t.TaskType == filter.TaskType.Value);
        if (filter.AssignedToUserIds.Count > 0)
            dbQuery = dbQuery.Where(t => t.AssignedToUserId != null && filter.AssignedToUserIds.Contains(t.AssignedToUserId.Value));
        else if (filter.AssignedToUserId.HasValue)
            dbQuery = dbQuery.Where(t => t.AssignedToUserId == filter.AssignedToUserId.Value);
        if (filter.MarketplaceClientIds.Count > 0)
            dbQuery = dbQuery.Where(t => filter.MarketplaceClientIds.Contains(t.MarketplaceClientId));
        else if (filter.MarketplaceClientId.HasValue)
            dbQuery = dbQuery.Where(t => t.MarketplaceClientId == filter.MarketplaceClientId.Value);

        var inProgressItems = await dbQuery
            .Where(t => t.Status == CommunicationTaskStatus.InProgress)
            .OrderByDescending(t => t.CreatedAt)
            .Select(ProjectToCardDto())
            .ToListAsync(ct);

        var doneQuery = dbQuery
            .Where(t => t.Status == CommunicationTaskStatus.Done ||
                        t.Status == CommunicationTaskStatus.Cancelled);

        if (filter.From.HasValue)
            doneQuery = doneQuery.Where(t => t.CreatedAt >= filter.From.Value);
        if (filter.To.HasValue)
            doneQuery = doneQuery.Where(t => t.CreatedAt <= filter.To.Value);

        var doneTotalCount = await doneQuery.CountAsync(ct);
        var doneItems = await doneQuery
            .OrderByDescending(t => t.CreatedAt)
            .Take(filter.DoneTake)
            .Select(ProjectToCardDto())
            .ToListAsync(ct);

        await EnrichChatCardsFromHistoryAsync(inProgressItems, ct);
        await EnrichChatCardsFromHistoryAsync(doneItems, ct);

        var doneTypeCounts = await doneQuery
            .GroupBy(t => t.TaskType)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var typeCounts = new Dictionary<CommunicationTaskType, int>();
        foreach (var tc in doneTypeCounts)
            typeCounts[tc.Key] = tc.Count;
        foreach (var t in inProgressItems)
            typeCounts[t.TaskType] = typeCounts.GetValueOrDefault(t.TaskType) + 1;

        var result = new TaskBoardDto { DoneTotalCount = doneTotalCount, TypeCounts = typeCounts };
        result.InProgressTasks.AddRange(inProgressItems);
        result.DoneTasks.AddRange(doneItems);

        var activeRates = await db.CommunicationPaymentRates
            .Include(r => r.User)
            .AsNoTracking()
            .Where(r => r.IsActive)
            .ToListAsync(ct);

        var globalRates = activeRates.Where(r => r.UserId == null).ToList();
        foreach (var type in new[] { CommunicationTaskType.Chat, CommunicationTaskType.Question, CommunicationTaskType.Review })
        {
            var matching = globalRates.Where(r => r.TaskType == null || r.TaskType == type).ToList();

            // Priority: if any specific rates exist for this type, exclude general "all types" rates from the estimate
            if (matching.Any(r => r.TaskType == type))
                matching = matching.Where(r => r.TaskType == type).ToList();

            var perTask = matching.Where(r => r.PaymentMode == PaymentMode.PerTask).Sum(r => r.Rate);
            // Bounded hourly rates: show full rate as max potential earnings (achieved at MaxDurationMinutes)
            var timedHourly = matching
                .Where(r => r.PaymentMode == PaymentMode.Hourly && r.MaxDurationMinutes.HasValue)
                .Sum(r => r.Rate);
            var fixedTotal = perTask + timedHourly;
            if (fixedTotal > 0) result.PaymentEstimates[type] = Math.Round(fixedTotal, 2);
            var hourly = matching
                .Where(r => r.PaymentMode == PaymentMode.Hourly && !r.MaxDurationMinutes.HasValue)
                .Sum(r => r.Rate);
            if (hourly > 0) result.HourlyEstimates[type] = Math.Round(hourly, 2);
        }
        result.ActiveRates.AddRange(activeRates.Select(r => new CommunicationPaymentRateDto
        {
            Id = r.Id,
            TaskType = r.TaskType,
            PaymentMode = r.PaymentMode,
            UserId = r.UserId,
            Rate = r.Rate,
            MinDurationMinutes = r.MinDurationMinutes,
            MaxDurationMinutes = r.MaxDurationMinutes,
            IsActive = r.IsActive,
            Description = r.Description
        }));

        return result;
    }

    public async Task<List<CommunicationTaskDto>> GetNewTasksAsync(CommunicationTaskFilter filter, CancellationToken ct = default)
    {
        if (filter.AssignedToUserId.HasValue) return new List<CommunicationTaskDto>();

        using var db = _dbContextFactory.CreateDbContext();
        db.ChangeTracker.Clear();

        var existingKeys = await LoadExistingTaskKeysForNewExclusionAsync(db, filter, ct);
        var newItems = new List<CommunicationTaskDto>();
        var noTypeFilter = !filter.TaskType.HasValue && filter.TaskTypes.Count == 0;
        var clientIds = filter.MarketplaceClientIds.Count > 0
            ? filter.MarketplaceClientIds
            : filter.MarketplaceClientId.HasValue ? new List<Guid> { filter.MarketplaceClientId.Value } : (List<Guid>?)null;

        if (noTypeFilter || filter.TaskTypes.Contains(CommunicationTaskType.Chat) || filter.TaskType == CommunicationTaskType.Chat)
        {
            if (clientIds is { Count: > 0 })
                foreach (var cid in clientIds)
                    newItems.AddRange(await FetchLiveChatsAsync(cid, existingKeys, filter.From, filter.To, ct));
            else
                newItems.AddRange(await FetchLiveChatsAsync(null, existingKeys, filter.From, filter.To, ct));
        }
        if (noTypeFilter || filter.TaskTypes.Contains(CommunicationTaskType.Question) || filter.TaskType == CommunicationTaskType.Question)
        {
            if (clientIds is { Count: > 0 })
                foreach (var cid in clientIds)
                    newItems.AddRange(await FetchLiveQuestionsAsync(cid, existingKeys, filter.From, filter.To, ct));
            else
                newItems.AddRange(await FetchLiveQuestionsAsync(null, existingKeys, filter.From, filter.To, ct));
        }
        if (noTypeFilter || filter.TaskTypes.Contains(CommunicationTaskType.Review) || filter.TaskType == CommunicationTaskType.Review)
        {
            if (clientIds is { Count: > 0 })
                foreach (var cid in clientIds)
                    newItems.AddRange(await FetchLiveReviewsAsync(cid, existingKeys, filter.From, filter.To, ct));
            else
                newItems.AddRange(await FetchLiveReviewsAsync(null, existingKeys, filter.From, filter.To, ct));
        }

        await EnrichChatCardsFromHistoryAsync(newItems, ct);
        return newItems;
    }

    public async IAsyncEnumerable<List<CommunicationTaskDto>> StreamNewTasksAsync(
        CommunicationTaskFilter filter,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (filter.AssignedToUserId.HasValue) yield break;

        using var db = _dbContextFactory.CreateDbContext();
        db.ChangeTracker.Clear();

        var existingKeys = await LoadExistingTaskKeysForNewExclusionAsync(db, filter, ct);
        var noTypeFilter = !filter.TaskType.HasValue && filter.TaskTypes.Count == 0;
        var clientIds = filter.MarketplaceClientIds.Count > 0
            ? filter.MarketplaceClientIds
            : filter.MarketplaceClientId.HasValue ? new List<Guid> { filter.MarketplaceClientId.Value } : (List<Guid>?)null;

        if (noTypeFilter || filter.TaskTypes.Contains(CommunicationTaskType.Chat) || filter.TaskType == CommunicationTaskType.Chat)
        {
            var batch = new List<CommunicationTaskDto>();
            if (clientIds is { Count: > 0 })
                foreach (var cid in clientIds)
                    batch.AddRange(await FetchLiveChatsAsync(cid, existingKeys, filter.From, filter.To, ct));
            else
                batch.AddRange(await FetchLiveChatsAsync(null, existingKeys, filter.From, filter.To, ct));
            if (batch.Count > 0) { await EnrichChatCardsFromHistoryAsync(batch, ct); yield return batch; }
        }

        if (noTypeFilter || filter.TaskTypes.Contains(CommunicationTaskType.Question) || filter.TaskType == CommunicationTaskType.Question)
        {
            var batch = new List<CommunicationTaskDto>();
            if (clientIds is { Count: > 0 })
                foreach (var cid in clientIds)
                    batch.AddRange(await FetchLiveQuestionsAsync(cid, existingKeys, filter.From, filter.To, ct));
            else
                batch.AddRange(await FetchLiveQuestionsAsync(null, existingKeys, filter.From, filter.To, ct));
            if (batch.Count > 0) yield return batch;
        }

        if (noTypeFilter || filter.TaskTypes.Contains(CommunicationTaskType.Review) || filter.TaskType == CommunicationTaskType.Review)
        {
            var batch = new List<CommunicationTaskDto>();
            if (clientIds is { Count: > 0 })
                foreach (var cid in clientIds)
                    batch.AddRange(await FetchLiveReviewsAsync(cid, existingKeys, filter.From, filter.To, ct));
            else
                batch.AddRange(await FetchLiveReviewsAsync(null, existingKeys, filter.From, filter.To, ct));
            if (batch.Count > 0) yield return batch;
        }
    }

    public async Task<(List<CommunicationTaskDto> Items, int TotalCount)> GetDoneTasksPageAsync(
        CommunicationTaskFilter filter, int skip, int take, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        db.ChangeTracker.Clear();

        var query = db.CommunicationTasks
            .AsNoTracking()
            .Where(t => !t.IsDeleted &&
                        (t.TaskType != CommunicationTaskType.Chat ||
                         t.ChatType == null ||
                         t.ChatType == "BUYER_SELLER") &&
                        (t.Status == CommunicationTaskStatus.Done ||
                         t.Status == CommunicationTaskStatus.Cancelled));

        if (filter.TaskTypes.Count > 0)
            query = query.Where(t => filter.TaskTypes.Contains(t.TaskType));
        else if (filter.TaskType.HasValue)
            query = query.Where(t => t.TaskType == filter.TaskType.Value);
        if (filter.AssignedToUserIds.Count > 0)
            query = query.Where(t => t.AssignedToUserId != null && filter.AssignedToUserIds.Contains(t.AssignedToUserId.Value));
        else if (filter.AssignedToUserId.HasValue)
            query = query.Where(t => t.AssignedToUserId == filter.AssignedToUserId.Value);
        if (filter.MarketplaceClientIds.Count > 0)
            query = query.Where(t => filter.MarketplaceClientIds.Contains(t.MarketplaceClientId));
        else if (filter.MarketplaceClientId.HasValue)
            query = query.Where(t => t.MarketplaceClientId == filter.MarketplaceClientId.Value);
        if (filter.From.HasValue)
            query = query.Where(t => t.CreatedAt >= filter.From.Value);
        if (filter.To.HasValue)
            query = query.Where(t => t.CreatedAt <= filter.To.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Select(ProjectToCardDto())
            .ToListAsync(ct);

        await EnrichChatCardsFromHistoryAsync(items, ct);

        return (items, total);
    }

    /// <summary>
    /// Projection expression used in board queries.
    /// Avoids loading related entities; computes HasActiveTimer via a correlated EXISTS.
    /// </summary>
    private static System.Linq.Expressions.Expression<Func<CommunicationTask, CommunicationTaskDto>> ProjectToCardDto()
    {
        return t => new CommunicationTaskDto
        {
            Id = t.Id,
            TaskType = t.TaskType,
            ExternalId = t.ExternalId,
            MarketplaceClientId = t.MarketplaceClientId,
            MarketplaceClientName = t.MarketplaceClient != null ? t.MarketplaceClient.Name : "—",
            Status = t.Status,
            AssignedToUserId = t.AssignedToUserId,
            AssignedToUserName = t.AssignedToUser != null
                ? t.AssignedToUser.FirstName + " " + t.AssignedToUser.LastName
                : null,
            AssignedAt = t.AssignedAt,
            CompletedAt = t.CompletedAt,
            Title = t.Title,
            PreviewText = t.PreviewText,
            ExternalStatus = t.ExternalStatus,
            ChatType = t.ChatType,
            UnreadCount = t.UnreadCount,
            ExternalUrl = t.ExternalUrl,
            PaymentAmount = t.PaymentAmount,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt,
            HasActiveTimer = t.TimeEntries.Any(e => e.EndedAt == null),
            StartedAt = t.TimeEntries
                .Where(e => e.EndedAt == null)
                .Select(e => (DateTime?)e.StartedAt)
                .FirstOrDefault(),
            TotalTimeSpentTicks = t.TotalTimeSpentTicks,
            LastMessageFromCustomer = false,
            WasPreviouslyCompleted = t.WasPreviouslyCompleted
        };
    }

    public async Task<CommunicationTaskDto?> FindActiveTaskAsync(string externalId, Guid marketplaceClientId, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        db.ChangeTracker.Clear();

        return await db.CommunicationTasks
            .AsNoTracking()
            .Where(t => !t.IsDeleted &&
                        t.Status == CommunicationTaskStatus.InProgress &&
                        t.ExternalId == externalId &&
                        t.MarketplaceClientId == marketplaceClientId)
            .Select(ProjectToCardDto())
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<CommunicationTaskDto>> GetInProgressTasksAsync(CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        db.ChangeTracker.Clear();

        return await db.CommunicationTasks
            .AsNoTracking()
            .Where(t => !t.IsDeleted && t.Status == CommunicationTaskStatus.InProgress)
            .Select(ProjectToCardDto())
            .ToListAsync(ct);
    }

    public async Task<List<CommunicationTaskDto>> GetDoneTasksTodayAsync(CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        db.ChangeTracker.Clear();
        var todayUtc = DateTime.UtcNow.Date;

        return await db.CommunicationTasks
            .AsNoTracking()
            .Where(t => !t.IsDeleted &&
                        t.Status == CommunicationTaskStatus.Done &&
                        t.CompletedAt >= todayUtc)
            .Select(ProjectToCardDto())
            .ToListAsync(ct);
    }

    public async Task<CommunicationTaskDetailDto?> GetTaskDetailAsync(Guid taskId, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        db.ChangeTracker.Clear();

        var task = await db.CommunicationTasks
            .Include(t => t.MarketplaceClient)
            .Include(t => t.AssignedToUser)
            .Include(t => t.Logs).ThenInclude(l => l.User)
            .Include(t => t.TimeEntries).ThenInclude(e => e.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == taskId, ct);

        if (task is null) return null;

        var dto = new CommunicationTaskDetailDto();
        CopyDtoFields(task, dto);

        dto.Logs = task.Logs.OrderByDescending(l => l.CreatedAt).Select(l => new CommunicationTaskLogDto
        {
            Id = l.Id,
            UserId = l.UserId,
            UserName = l.User != null ? $"{l.User.FirstName} {l.User.LastName}" : null,
            Action = l.Action,
            Details = l.Details,
            CreatedAt = l.CreatedAt
        }).ToList();

        dto.TimeEntries = task.TimeEntries.OrderByDescending(e => e.StartedAt).Select(e => new CommunicationTimeEntryDto
        {
            Id = e.Id,
            UserId = e.UserId,
            UserName = e.User != null ? $"{e.User.FirstName} {e.User.LastName}" : "—",
            StartedAt = e.StartedAt,
            EndedAt = e.EndedAt,
            Note = e.Note,
            DurationMinutes = e.Duration.TotalMinutes
        }).ToList();

        return dto;
    }

    public async Task<Guid?> CreateAndClaimAsync(CommunicationTaskDto liveTask, Guid userId, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        db.ChangeTracker.Clear();
        db.SuppressAudit = true;
        try
        {
            userId = await ResolveUserIdAsync(db, userId, ct);

            // Race guard: another user may have already claimed this Ozon item.
            var existing = await db.CommunicationTasks
                .Include(t => t.TimeEntries)
                .FirstOrDefaultAsync(t =>
                    t.TaskType == liveTask.TaskType &&
                    t.ExternalId == liveTask.ExternalId &&
                    t.MarketplaceClientId == liveTask.MarketplaceClientId &&
                    !t.IsDeleted, ct);

            if (existing is not null)
            {
                if (existing.AssignedToUserId is not null && existing.AssignedToUserId != userId)
                    return null; // already claimed by someone else

                // Already our task — ensure timer is running
                existing.AssignedToUserId = userId;
                existing.AssignedAt = DateTime.UtcNow;
                existing.Status = CommunicationTaskStatus.InProgress;
                existing.StartedAt ??= DateTime.UtcNow;
                existing.UpdatedAt = DateTime.UtcNow;

                if (!existing.TimeEntries.Any(e => e.EndedAt == null))
                {
                    db.CommunicationTimeEntries.Add(new CommunicationTimeEntry
                    {
                        Id = Guid.NewGuid(),
                        TaskId = existing.Id,
                        UserId = userId,
                        StartedAt = DateTime.UtcNow
                    });
                }

                await db.SaveChangesAsync(ct);
                return existing.Id;
            }

            var task = new CommunicationTask
            {
                Id = Guid.NewGuid(),
                TaskType = liveTask.TaskType,
                ExternalId = liveTask.ExternalId,
                MarketplaceClientId = liveTask.MarketplaceClientId,
                Status = CommunicationTaskStatus.InProgress,
                AssignedToUserId = userId,
                AssignedAt = DateTime.UtcNow,
                StartedAt = DateTime.UtcNow,
                Title = liveTask.Title,
                PreviewText = liveTask.PreviewText,
                ExternalStatus = liveTask.ExternalStatus,
                ChatType = liveTask.ChatType,
                UnreadCount = liveTask.UnreadCount,
                ExternalUrl = liveTask.ExternalUrl,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.CommunicationTasks.Add(task);

            db.CommunicationTimeEntries.Add(new CommunicationTimeEntry
            {
                Id = Guid.NewGuid(),
                TaskId = task.Id,
                UserId = userId,
                StartedAt = DateTime.UtcNow
            });

            task.Logs.Add(new CommunicationTaskLog
            {
                Id = Guid.NewGuid(),
                TaskId = task.Id,
                UserId = userId,
                Action = "Assigned",
                CreatedAt = DateTime.UtcNow
            });
            task.Logs.Add(new CommunicationTaskLog
            {
                Id = Guid.NewGuid(),
                TaskId = task.Id,
                UserId = userId,
                Action = "Started",
                CreatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync(ct);
            _logger.LogInformation("Task {ExternalId} ({Type}) created and claimed by user {UserId}",
                liveTask.ExternalId, liveTask.TaskType, userId);
            await NotifyBoardChangedAsync(ct);
            return task.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateAndClaimAsync failed for {ExternalId}", liveTask.ExternalId);
            return null;
        }
        finally
        {
            db.SuppressAudit = false;
        }
    }

    public async Task<bool> ClaimTaskAsync(Guid taskId, Guid userId, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        db.ChangeTracker.Clear();
        db.SuppressAudit = true;
        try
        {
            userId = await ResolveUserIdAsync(db, userId, ct);

            var task = await db.CommunicationTasks
                .Include(t => t.TimeEntries)
                .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted, ct);

            if (task is null) return false;
            if (task.AssignedToUserId is not null && task.AssignedToUserId != userId) return false;

            task.AssignedToUserId = userId;
            task.AssignedAt = DateTime.UtcNow;
            task.Status = CommunicationTaskStatus.InProgress;
            task.StartedAt ??= DateTime.UtcNow;
            task.UpdatedAt = DateTime.UtcNow;

            db.CommunicationTimeEntries.Add(new CommunicationTimeEntry
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                UserId = userId,
                StartedAt = DateTime.UtcNow
            });

            db.CommunicationTaskLogs.Add(new CommunicationTaskLog
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                UserId = userId,
                Action = "Assigned",
                CreatedAt = DateTime.UtcNow
            });
            db.CommunicationTaskLogs.Add(new CommunicationTaskLog
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                UserId = userId,
                Action = "Started",
                CreatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync(ct);
            _logger.LogInformation("Task {TaskId} claimed by user {UserId}", taskId, userId);
            await NotifyBoardChangedAsync(ct);
            return true;
        }
        finally
        {
            db.SuppressAudit = false;
        }
    }

    public async Task<bool> ReleaseTaskAsync(Guid taskId, Guid userId, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        db.ChangeTracker.Clear();
        db.SuppressAudit = true;
        try
        {
            var task = await db.CommunicationTasks
                .Include(t => t.TimeEntries)
                .Include(t => t.Logs)
                .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted, ct);

            if (task is null || task.AssignedToUserId != userId) return false;

            // Delete the record so the task returns to the live Ozon feed.
            db.CommunicationTasks.Remove(task);
            await db.SaveChangesAsync(ct);
            _logger.LogInformation("Task {TaskId} released and removed from DB by user {UserId}", taskId, userId);
            await NotifyBoardChangedAsync(ct);
            return true;
        }
        finally
        {
            db.SuppressAudit = false;
        }
    }

    public async Task<bool> PauseTaskAsync(Guid taskId, Guid userId, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        db.ChangeTracker.Clear();
        db.SuppressAudit = true;
        try
        {
            userId = await ResolveUserIdAsync(db, userId, ct);

            var task = await db.CommunicationTasks
                .Include(t => t.TimeEntries)
                .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted, ct);

            if (task is null || task.AssignedToUserId != userId) return false;

            StopActiveTimers(task, userId);

            // Keep accumulated time up to date so the board DTO always shows correct total.
            task.TotalTimeSpentTicks = task.TimeEntries
                .Where(e => e.EndedAt.HasValue)
                .Sum(e => (e.EndedAt!.Value - e.StartedAt).Ticks);

            db.CommunicationTaskLogs.Add(new CommunicationTaskLog
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                UserId = userId,
                Action = "Paused",
                CreatedAt = DateTime.UtcNow
            });

            task.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return true;
        }
        finally
        {
            db.SuppressAudit = false;
        }
    }

    public async Task<bool> ResumeTaskAsync(Guid taskId, Guid userId, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        db.ChangeTracker.Clear();
        db.SuppressAudit = true;
        try
        {
            userId = await ResolveUserIdAsync(db, userId, ct);

            var task = await db.CommunicationTasks
                .Include(t => t.TimeEntries)
                .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted, ct);

            if (task is null || task.AssignedToUserId != userId) return false;

            var hasActive = task.TimeEntries.Any(e => e.UserId == userId && e.EndedAt == null);
            if (hasActive) return true;

            db.CommunicationTimeEntries.Add(new CommunicationTimeEntry
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                UserId = userId,
                StartedAt = DateTime.UtcNow
            });

            db.CommunicationTaskLogs.Add(new CommunicationTaskLog
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                UserId = userId,
                Action = "Resumed",
                CreatedAt = DateTime.UtcNow
            });

            task.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return true;
        }
        finally
        {
            db.SuppressAudit = false;
        }
    }

    public async Task<bool> CompleteTaskAsync(Guid taskId, Guid userId, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        db.ChangeTracker.Clear();
        db.SuppressAudit = true;
        try
        {
            userId = await ResolveUserIdAsync(db, userId, ct);

            var task = await db.CommunicationTasks
                .Include(t => t.TimeEntries)
                .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted, ct);

            if (task is null) return false;
            if (task.AssignedToUserId is not null && task.AssignedToUserId != userId) return false;

            StopActiveTimers(task, userId);

            var totalTicks = task.TimeEntries
                .Where(e => e.EndedAt.HasValue)
                .Sum(e => (e.EndedAt!.Value - e.StartedAt).Ticks);
            task.TotalTimeSpentTicks = totalTicks;

            task.Status = CommunicationTaskStatus.Done;
            task.CompletedAt = DateTime.UtcNow;
            task.UpdatedAt = DateTime.UtcNow;

            // Ensure AssignedToUserId is set so personal payment rates are applied correctly.
            task.AssignedToUserId ??= userId;

            task.PaymentAmount = await CalculatePaymentAsync(db, task, ct);

            var ru = CultureInfo.GetCultureInfo("ru-RU");
            var payStr = (task.PaymentAmount ?? 0m).ToString("N2", ru);
            var timeStr = string.Format(CultureInfo.InvariantCulture, "{0:hh\\:mm\\:ss}", TimeSpan.FromTicks(totalTicks));
            db.CommunicationTaskLogs.Add(new CommunicationTaskLog
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                UserId = userId,
                Action = "Completed",
                Details = $"Время в работе: {timeStr}, начислено: {payStr} ₽",
                CreatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync(ct);
            _logger.LogInformation("Task {TaskId} completed by user {UserId}, payment={Payment}",
                taskId, userId, task.PaymentAmount);
            await NotifyBoardChangedAsync(ct);
            return true;
        }
        finally
        {
            db.SuppressAudit = false;
        }
    }

    public async Task<bool> ReopenTaskAsync(Guid taskId, Guid userId, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        db.ChangeTracker.Clear();
        db.SuppressAudit = true;
        try
        {
            userId = await ResolveUserIdAsync(db, userId, ct);

            var task = await db.CommunicationTasks
                .Include(t => t.TimeEntries)
                .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted, ct);

            if (task is null) return false;
            if (task.Status != CommunicationTaskStatus.Done) return false;
            if (task.AssignedToUserId != userId) return false;

            task.Status = CommunicationTaskStatus.InProgress;
            task.CompletedAt = null;
            task.PaymentAmount = null;
            task.UpdatedAt = DateTime.UtcNow;

            var hasActive = task.TimeEntries.Any(e => e.UserId == userId && e.EndedAt == null);
            if (!hasActive)
            {
                db.CommunicationTimeEntries.Add(new CommunicationTimeEntry
                {
                    Id = Guid.NewGuid(),
                    TaskId = taskId,
                    UserId = userId,
                    StartedAt = DateTime.UtcNow
                });
            }

            db.CommunicationTaskLogs.Add(new CommunicationTaskLog
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                UserId = userId,
                Action = "Reopened",
                CreatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync(ct);
            _logger.LogInformation("Task {TaskId} reopened by user {UserId}", taskId, userId);
            await NotifyBoardChangedAsync(ct);
            return true;
        }
        finally
        {
            db.SuppressAudit = false;
        }
    }

    public async Task<PaymentReportDto> GetPaymentReportAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();

        var doneTasks = await db.CommunicationTasks
            .Include(t => t.AssignedToUser)
            .AsNoTracking()
            .Where(t => t.Status == CommunicationTaskStatus.Done &&
                        t.CompletedAt >= from && t.CompletedAt <= to &&
                        t.AssignedToUserId != null)
            .ToListAsync(ct);

        var grouped = doneTasks
            .GroupBy(t => t.AssignedToUserId!.Value)
            .Select(g =>
            {
                var user = g.First().AssignedToUser;
                return new UserTaskStatsDto
                {
                    UserId = g.Key,
                    UserName = user != null ? $"{user.FirstName} {user.LastName}" : "—",
                    ChatsDone = g.Count(t => t.TaskType == CommunicationTaskType.Chat),
                    QuestionsDone = g.Count(t => t.TaskType == CommunicationTaskType.Question),
                    ReviewsDone = g.Count(t => t.TaskType == CommunicationTaskType.Review),
                    TotalHours = g.Sum(t => TimeSpan.FromTicks(t.TotalTimeSpentTicks).TotalHours),
                    TotalPayment = g.Sum(t => t.PaymentAmount ?? 0m)
                };
            }).ToList();

        return new PaymentReportDto
        {
            Users = grouped,
            TotalTasks = doneTasks.Count,
            TotalHours = grouped.Sum(u => u.TotalHours),
            TotalPayment = grouped.Sum(u => u.TotalPayment)
        };
    }

    public async Task<UserTaskDetailsDto> GetUserTaskDetailsAsync(Guid userId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();

        var tasks = await db.CommunicationTasks
            .Include(t => t.AssignedToUser)
            .Include(t => t.MarketplaceClient)
            .Include(t => t.TimeEntries)
            .AsNoTracking()
            .Where(t => t.Status == CommunicationTaskStatus.Done &&
                        t.AssignedToUserId == userId &&
                        t.CompletedAt >= from && t.CompletedAt <= to &&
                        !t.IsDeleted)
            .OrderByDescending(t => t.CompletedAt)
            .ToListAsync(ct);

        var user = tasks.FirstOrDefault()?.AssignedToUser;
        var userName = user != null ? $"{user.FirstName} {user.LastName}".Trim() : userId.ToString();

        var items = tasks.Select(t => new ReportTaskItemDto
        {
            Id = t.Id,
            TaskType = t.TaskType,
            Title = t.Title,
            PreviewText = t.PreviewText,
            ExternalStatus = t.ExternalStatus,
            ExternalUrl = t.ExternalUrl,
            ExternalId = t.ExternalId,
            MarketplaceClientId = t.MarketplaceClientId,
            MarketplaceClientName = t.MarketplaceClient?.Name ?? "—",
            StartedAt = t.StartedAt,
            CompletedAt = t.CompletedAt,
            TotalTimeSpentTicks = t.TotalTimeSpentTicks,
            PaymentAmount = t.PaymentAmount,
            TimeEntries = t.TimeEntries
                .Where(e => e.EndedAt.HasValue)
                .OrderBy(e => e.StartedAt)
                .Select(e => new ReportTimeEntryDto
                {
                    StartedAt = e.StartedAt,
                    EndedAt = e.EndedAt,
                    DurationMinutes = (e.EndedAt!.Value - e.StartedAt).TotalMinutes,
                    Note = e.Note
                }).ToList()
        }).ToList();

        var rateDtos = await db.CommunicationPaymentRates
            .AsNoTracking()
            .Where(r => r.IsActive)
            .Select(r => new CommunicationPaymentRateDto
            {
                Id = r.Id,
                TaskType = r.TaskType,
                PaymentMode = r.PaymentMode,
                UserId = r.UserId,
                Rate = r.Rate,
                MinDurationMinutes = r.MinDurationMinutes,
                MaxDurationMinutes = r.MaxDurationMinutes,
                IsActive = r.IsActive,
                Description = r.Description
            })
            .ToListAsync(ct);

        foreach (var item in items)
        {
            var totalMinutes = (decimal)TimeSpan.FromTicks(item.TotalTimeSpentTicks).TotalMinutes;
            item.PaymentBreakdown = CommunicationPaymentCalculator.ComputeBreakdownLines(
                totalMinutes, item.TaskType, userId, rateDtos);
        }

        return new UserTaskDetailsDto { UserId = userId, UserName = userName, Tasks = items };
    }

    public async Task<PersonalStatsDto> GetPersonalStatsAsync(Guid userId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();

        var tasks = await db.CommunicationTasks
            .Include(t => t.MarketplaceClient)
            .Include(t => t.TimeEntries)
            .AsNoTracking()
            .Where(t => t.Status == CommunicationTaskStatus.Done &&
                        t.AssignedToUserId == userId &&
                        t.CompletedAt >= from && t.CompletedAt <= to &&
                        !t.IsDeleted)
            .OrderByDescending(t => t.CompletedAt)
            .ToListAsync(ct);

        var user = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.FirstName, u.LastName })
            .FirstOrDefaultAsync(ct);

        var userName = user != null ? $"{user.FirstName} {user.LastName}".Trim() : userId.ToString();

        // Build daily activity, filling all days in range (including empty days)
        var byDay = tasks
            .GroupBy(t => t.CompletedAt!.Value.Date)
            .ToDictionary(g => g.Key, g => g.ToList());

        var allDays = new List<DailyActivityDto>();
        for (var d = from.Date; d <= to.Date; d = d.AddDays(1))
        {
            if (byDay.TryGetValue(d, out var dayTasks))
            {
                allDays.Add(new DailyActivityDto
                {
                    Date = d,
                    ChatsDone = dayTasks.Count(t => t.TaskType == CommunicationTaskType.Chat),
                    QuestionsDone = dayTasks.Count(t => t.TaskType == CommunicationTaskType.Question),
                    ReviewsDone = dayTasks.Count(t => t.TaskType == CommunicationTaskType.Review),
                    TotalHours = dayTasks.Sum(t => TimeSpan.FromTicks(t.TotalTimeSpentTicks).TotalHours),
                    TotalPayment = dayTasks.Sum(t => t.PaymentAmount ?? 0m)
                });
            }
            else
            {
                allDays.Add(new DailyActivityDto { Date = d });
            }
        }

        var recentTasks = tasks.Take(30).Select(t => new ReportTaskItemDto
        {
            Id = t.Id,
            TaskType = t.TaskType,
            Title = t.Title,
            PreviewText = t.PreviewText,
            ExternalStatus = t.ExternalStatus,
            ExternalUrl = t.ExternalUrl,
            ExternalId = t.ExternalId,
            MarketplaceClientId = t.MarketplaceClientId,
            MarketplaceClientName = t.MarketplaceClient?.Name ?? "—",
            StartedAt = t.StartedAt,
            CompletedAt = t.CompletedAt,
            TotalTimeSpentTicks = t.TotalTimeSpentTicks,
            PaymentAmount = t.PaymentAmount,
            TimeEntries = t.TimeEntries
                .Where(e => e.EndedAt.HasValue)
                .OrderBy(e => e.StartedAt)
                .Select(e => new ReportTimeEntryDto
                {
                    StartedAt = e.StartedAt,
                    EndedAt = e.EndedAt,
                    DurationMinutes = (e.EndedAt!.Value - e.StartedAt).TotalMinutes,
                    Note = e.Note
                }).ToList()
        }).ToList();

        return new PersonalStatsDto
        {
            UserId = userId,
            UserName = userName,
            ChatsDone = tasks.Count(t => t.TaskType == CommunicationTaskType.Chat),
            QuestionsDone = tasks.Count(t => t.TaskType == CommunicationTaskType.Question),
            ReviewsDone = tasks.Count(t => t.TaskType == CommunicationTaskType.Review),
            TotalHours = tasks.Sum(t => TimeSpan.FromTicks(t.TotalTimeSpentTicks).TotalHours),
            TotalPayment = tasks.Sum(t => t.PaymentAmount ?? 0m),
            DailyActivity = allDays,
            RecentTasks = recentTasks
        };
    }

    public async Task<List<CommunicationPaymentRateDto>> GetPaymentRatesAsync(CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();

        return await db.CommunicationPaymentRates
            .Include(r => r.User)
            .AsNoTracking()
            .OrderBy(r => r.TaskType).ThenBy(r => r.PaymentMode).ThenBy(r => r.UserId)
            .Select(r => new CommunicationPaymentRateDto
            {
                Id = r.Id,
                TaskType = r.TaskType,
                PaymentMode = r.PaymentMode,
                UserId = r.UserId,
                UserName = r.User != null ? r.User.FirstName + " " + r.User.LastName : null,
                Rate = r.Rate,
                MinDurationMinutes = r.MinDurationMinutes,
                MaxDurationMinutes = r.MaxDurationMinutes,
                IsActive = r.IsActive,
                Description = r.Description
            })
            .ToListAsync(ct);
    }

    public async Task<CommunicationPaymentRateDto> SavePaymentRateAsync(CommunicationPaymentRateDto dto, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        db.ChangeTracker.Clear();
        db.SuppressAudit = true;
        try
        {
            CommunicationPaymentRate entity;

            if (dto.Id == Guid.Empty)
            {
                entity = new CommunicationPaymentRate { Id = Guid.NewGuid() };
                db.CommunicationPaymentRates.Add(entity);
            }
            else
            {
                entity = await db.CommunicationPaymentRates.FindAsync(new object[] { dto.Id }, ct)
                         ?? throw new InvalidOperationException($"Rate {dto.Id} not found");
            }

            entity.TaskType = dto.TaskType;
            entity.PaymentMode = dto.PaymentMode;
            entity.UserId = dto.UserId;
            entity.Rate = dto.Rate;
            entity.MinDurationMinutes = dto.MinDurationMinutes;
            entity.MaxDurationMinutes = dto.MaxDurationMinutes;
            entity.IsActive = dto.IsActive;
            entity.Description = dto.Description;

            await db.SaveChangesAsync(ct);

            dto.Id = entity.Id;
            return dto;
        }
        finally
        {
            db.SuppressAudit = false;
        }
    }

    public async Task<bool> DeletePaymentRateAsync(Guid id, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        db.ChangeTracker.Clear();

        var entity = await db.CommunicationPaymentRates.FindAsync(new object[] { id }, ct);
        if (entity is null) return false;

        db.CommunicationPaymentRates.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private async Task NotifyBoardChangedAsync(CancellationToken ct)
    {
        try
        {
            var tenantId = _tenantProvider.TenantId;
            if (tenantId.HasValue && tenantId.Value != Guid.Empty)
                await _notificationSender.SendBoardUpdatedAsync(tenantId.Value, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send board update notification");
        }
    }

    public async Task<int> RecalculatePaymentsAsync(CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        db.ChangeTracker.Clear();
        db.SuppressAudit = true;
        try
        {
            var done = await db.CommunicationTasks
                .Include(t => t.TimeEntries)
                .Where(t => t.Status == CommunicationTaskStatus.Done && !t.IsDeleted)
                .ToListAsync(ct);

            foreach (var task in done)
            {
                // Rebuild total worked time from closed segments so legacy/inconsistent values
                // do not affect recalculation by the new payment formula.
                task.TotalTimeSpentTicks = task.TimeEntries
                    .Where(e => e.EndedAt.HasValue)
                    .Sum(e => (e.EndedAt!.Value - e.StartedAt).Ticks);
                task.PaymentAmount = await CalculatePaymentAsync(db, task, ct);
                task.UpdatedAt = DateTime.UtcNow;
            }

            await db.SaveChangesAsync(ct);
            _logger.LogInformation("RecalculatePayments: updated {Count} tasks", done.Count);
            await NotifyBoardChangedAsync(ct);
            return done.Count;
        }
        finally
        {
            db.SuppressAudit = false;
        }
    }

    public async Task RecordOutgoingMessageAsync(
        string externalId,
        CommunicationTaskType taskType,
        string externalMessageId,
        Guid sentByUserId,
        string sentByUserName,
        CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        db.SuppressAudit = true;
        try
        {
            var exists = await db.CommunicationOutgoingMessages
                .AnyAsync(m => m.ExternalId == externalId &&
                               m.TaskType == taskType &&
                               m.ExternalMessageId == externalMessageId, ct);
            if (exists) return;

            db.CommunicationOutgoingMessages.Add(new CommunicationOutgoingMessage
            {
                Id = Guid.NewGuid(),
                ExternalId = externalId,
                TaskType = taskType,
                ExternalMessageId = externalMessageId,
                SentByUserId = sentByUserId,
                SentByUserName = sentByUserName,
                SentAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync(ct);
        }
        finally
        {
            db.SuppressAudit = false;
        }
    }

    public async Task<Dictionary<string, string>> GetOutgoingAuthorsAsync(
        string externalId,
        CommunicationTaskType taskType,
        CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        var rows = await db.CommunicationOutgoingMessages
            .AsNoTracking()
            .Where(m => m.ExternalId == externalId && m.TaskType == taskType)
            .Select(m => new { m.ExternalMessageId, m.SentByUserName })
            .ToListAsync(ct);

        return rows.ToDictionary(r => r.ExternalMessageId, r => r.SentByUserName);
    }


    private static async Task<decimal> CalculatePaymentAsync(TenantDbContext db, CommunicationTask task, CancellationToken ct)
    {
        var taskType = task.TaskType;
        var assignedUserId = task.AssignedToUserId;
        var totalMinutes = (decimal)TimeSpan.FromTicks(task.TotalTimeSpentTicks).TotalMinutes;

        var rates = await db.CommunicationPaymentRates
            .AsNoTracking()
            .Where(r => r.IsActive)
            .ToListAsync(ct);

        if (rates.Count == 0) return 0m;

        var rateDtos = rates.Select(r => new CommunicationPaymentRateDto
        {
            Id = r.Id,
            TaskType = r.TaskType,
            PaymentMode = r.PaymentMode,
            UserId = r.UserId,
            Rate = r.Rate,
            MinDurationMinutes = r.MinDurationMinutes,
            MaxDurationMinutes = r.MaxDurationMinutes,
            IsActive = r.IsActive,
            Description = r.Description
        }).ToList();

        var lines = CommunicationPaymentCalculator.ComputeBreakdownLines(
            totalMinutes, taskType, assignedUserId, rateDtos);

        return Math.Round(lines.Sum(l => l.Amount), 2);
    }

    /// <summary>
    /// Live «новые» чаты: первая страница как на «Чаты», только <c>BUYER_SELLER</c>.
    /// </summary>
    private async Task<List<CommunicationTaskDto>> FetchLiveChatsAsync(
        Guid? marketplaceClientId,
        HashSet<(CommunicationTaskType, string, Guid)> existingTaskKeys,
        DateTime? from, DateTime? to,
        CancellationToken ct)
    {
        try
        {
            var clientKey = marketplaceClientId?.ToString() ?? "all";
            var cacheKey = $"ozon_chats_board:{_tenantProvider.TenantId}:{clientKey}:BUYER_SELLER";
            _liveCacheKeys.Add(cacheKey);

            var allChats = await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
                return await LoadChatsFirstPageLikeChatsPageAsync(marketplaceClientId, ct);
            }) ?? [];

            var toExclusive = to.HasValue ? to.Value.Date.AddDays(1) : DateTime.MaxValue;

            var result = new List<CommunicationTaskDto>();
            foreach (var chat in allChats)
            {
                if (existingTaskKeys.Contains((CommunicationTaskType.Chat, chat.ChatId, chat.MarketplaceClientId)))
                    continue;
                if (from.HasValue && chat.LastMessageAt < from.Value)
                    continue;
                if (chat.LastMessageAt >= toExclusive)
                    continue;

                result.Add(new CommunicationTaskDto
                {
                    Id = Guid.Empty,
                    TaskType = CommunicationTaskType.Chat,
                    ExternalId = chat.ChatId,
                    MarketplaceClientId = chat.MarketplaceClientId,
                    MarketplaceClientName = chat.MarketplaceClientName,
                    Status = CommunicationTaskStatus.New,
                    Title = $"Чат — {chat.MarketplaceClientName}",
                    ExternalStatus = chat.ChatStatus,
                    ChatType = chat.ChatType,
                    UnreadCount = chat.UnreadCount,
                    CreatedAt = chat.LastMessageAt,
                    UpdatedAt = DateTime.UtcNow,
                    LastMessageFromCustomer = IsOzonCustomerLastMessageUserType(chat.LastMessageUserType)
                });
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GetBoardAsync: failed to fetch live chats from Ozon");
            return [];
        }
    }

    /// <summary>
    /// Как первая страница списка на «Чаты», но только тип «Покупатель — Продавец» (<c>BUYER_SELLER</c>), как пилюля на той странице.
    /// </summary>
    private async Task<List<OzonChatViewModelDto>> LoadChatsFirstPageLikeChatsPageAsync(
        Guid? marketplaceClientId,
        CancellationToken ct)
    {
        var page = await _chatService.GetChatsPageAsync(
            pageSize: 20,
            cursor: null,
            chatStatus: null,
            chatType: "BUYER_SELLER",
            unreadOnly: false,
            marketplaceClientId: marketplaceClientId,
            withLastMessageInfo: true,
            ct: ct);
        return page.Chats;
    }

    private async Task<List<CommunicationTaskDto>> FetchLiveQuestionsAsync(
        Guid? marketplaceClientId,
        HashSet<(CommunicationTaskType, string, Guid)> existingTaskKeys,
        DateTime? from, DateTime? to,
        CancellationToken ct)
    {
        try
        {
            var toExclusive = to.HasValue ? (DateTime?)to.Value.Date.AddDays(1) : null;
            var fromKey = from?.ToString("yyyyMMdd") ?? "null";
            var toKey = toExclusive?.ToString("yyyyMMdd") ?? "null";
            var clientKey = marketplaceClientId?.ToString() ?? "all";
            var cacheKey = $"ozon_questions_board:{_tenantProvider.TenantId}:{clientKey}:{fromKey}:{toKey}";
            _liveCacheKeys.Add(cacheKey);

            var page = await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
                return await _questionsService.GetQuestionsPageAsync(
                    pageSize: 20,
                    cursor: null,
                    dateFrom: from,
                    dateTo: toExclusive,
                    status: null,
                    marketplaceClientId: marketplaceClientId,
                    ct: ct);
            }) ?? new OzonQuestionPageDto();

            var result = new List<CommunicationTaskDto>();
            foreach (var q in page.Questions)
            {
                if (string.Equals(q.Status, "PROCESSED", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (existingTaskKeys.Contains((CommunicationTaskType.Question, q.Id, q.MarketplaceClientId)))
                    continue;

                result.Add(new CommunicationTaskDto
                {
                    Id = Guid.Empty,
                    TaskType = CommunicationTaskType.Question,
                    ExternalId = q.Id,
                    MarketplaceClientId = q.MarketplaceClientId,
                    MarketplaceClientName = q.MarketplaceClientName,
                    Status = CommunicationTaskStatus.New,
                    Title = $"Вопрос — {q.MarketplaceClientName}",
                    PreviewText = q.Text.Length > 200 ? q.Text[..200] + "..." : q.Text,
                    ExternalStatus = q.Status,
                    ExternalUrl = q.QuestionLink,
                    CreatedAt = q.PublishedAt,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GetBoardAsync: failed to fetch live questions from Ozon");
            return [];
        }
    }

    private async Task<List<CommunicationTaskDto>> FetchLiveReviewsAsync(
        Guid? marketplaceClientId,
        HashSet<(CommunicationTaskType, string, Guid)> existingTaskKeys,
        DateTime? from, DateTime? to,
        CancellationToken ct)
    {
        try
        {
            var clientKey = marketplaceClientId?.ToString() ?? "all";
            var cacheKey = $"ozon_reviews_board:{_tenantProvider.TenantId}:{clientKey}";
            _liveCacheKeys.Add(cacheKey);

            var page = await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
                return await _reviewsService.GetReviewsPageAsync(
                    pageSize: 20,
                    cursor: null,
                    status: null,
                    marketplaceClientId: marketplaceClientId,
                    ct: ct);
            }) ?? new OzonReviewPageDto();

            var toExclusive = to.HasValue ? to.Value.Date.AddDays(1) : DateTime.MaxValue;

            var result = new List<CommunicationTaskDto>();
            foreach (var r in page.Reviews)
            {
                if (string.Equals(r.Status, "PROCESSED", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (existingTaskKeys.Contains((CommunicationTaskType.Review, r.Id, r.MarketplaceClientId)))
                    continue;
                if (from.HasValue && r.PublishedAt < from.Value)
                    continue;
                if (r.PublishedAt >= toExclusive)
                    continue;

                result.Add(new CommunicationTaskDto
                {
                    Id = Guid.Empty,
                    TaskType = CommunicationTaskType.Review,
                    ExternalId = r.Id,
                    MarketplaceClientId = r.MarketplaceClientId,
                    MarketplaceClientName = r.MarketplaceClientName,
                    Status = CommunicationTaskStatus.New,
                    Title = $"Отзыв ({r.Rating}★) — {r.MarketplaceClientName}",
                    PreviewText = r.Text.Length > 200 ? r.Text[..200] + "..." : r.Text,
                    ExternalStatus = r.Status,
                    CreatedAt = r.PublishedAt,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GetBoardAsync: failed to fetch live reviews from Ozon");
            return [];
        }
    }

    private static void StopActiveTimers(CommunicationTask task, Guid userId)
    {
        foreach (var entry in task.TimeEntries.Where(e => e.UserId == userId && e.EndedAt == null))
        {
            entry.EndedAt = DateTime.UtcNow;
        }
    }

    private static CommunicationTaskDto MapToDto(CommunicationTask t)
    {
        var dto = new CommunicationTaskDto();
        CopyDtoFields(t, dto);
        return dto;
    }

    private static void CopyDtoFields(CommunicationTask t, CommunicationTaskDto dto)
    {
        dto.Id = t.Id;
        dto.TaskType = t.TaskType;
        dto.ExternalId = t.ExternalId;
        dto.MarketplaceClientId = t.MarketplaceClientId;
        dto.MarketplaceClientName = t.MarketplaceClient?.Name ?? "—";
        dto.Status = t.Status;
        dto.AssignedToUserId = t.AssignedToUserId;
        dto.AssignedToUserName = t.AssignedToUser != null
            ? $"{t.AssignedToUser.FirstName} {t.AssignedToUser.LastName}"
            : null;
        dto.AssignedAt = t.AssignedAt;
        dto.CompletedAt = t.CompletedAt;
        // StartedAt = beginning of the current ACTIVE segment (null when paused).
        dto.StartedAt = t.TimeEntries?.FirstOrDefault(e => e.EndedAt == null)?.StartedAt;
        // TotalTimeSpentTicks = sum of all CLOSED segments (kept up to date after each Pause).
        dto.TotalTimeSpentTicks = t.TimeEntries is { Count: > 0 }
            ? t.TimeEntries.Where(e => e.EndedAt.HasValue).Sum(e => (e.EndedAt!.Value - e.StartedAt).Ticks)
            : t.TotalTimeSpentTicks;
        dto.Title = t.Title;
        dto.PreviewText = t.PreviewText;
        dto.ExternalStatus = t.ExternalStatus;
        dto.ChatType = t.ChatType;
        dto.UnreadCount = t.UnreadCount;
        dto.ExternalUrl = t.ExternalUrl;
        dto.PaymentAmount = t.PaymentAmount;
        dto.CreatedAt = t.CreatedAt;
        dto.UpdatedAt = t.UpdatedAt;
        dto.HasActiveTimer = t.TimeEntries?.Any(e => e.EndedAt == null) ?? false;
        dto.LastMessageFromCustomer = false;
        dto.WasPreviouslyCompleted = t.WasPreviouslyCompleted;
    }

    private static async Task<HashSet<(CommunicationTaskType TaskType, string ExternalId, Guid MarketplaceClientId)>>
        LoadExistingTaskKeysForNewExclusionAsync(TenantDbContext db, CommunicationTaskFilter filter, CancellationToken ct)
    {
        var q = db.CommunicationTasks
            .AsNoTracking()
            .Where(t => !t.IsDeleted &&
                        (t.Status == CommunicationTaskStatus.InProgress ||
                         t.Status == CommunicationTaskStatus.Done ||
                         t.Status == CommunicationTaskStatus.Cancelled) &&
                        (t.TaskType != CommunicationTaskType.Chat ||
                         t.ChatType == null ||
                         t.ChatType == "BUYER_SELLER"));

        if (filter.TaskType.HasValue)
            q = q.Where(t => t.TaskType == filter.TaskType.Value);
        if (filter.MarketplaceClientId.HasValue)
            q = q.Where(t => t.MarketplaceClientId == filter.MarketplaceClientId.Value);

        var rows = await q.Select(t => new { t.TaskType, t.ExternalId, t.MarketplaceClientId }).ToListAsync(ct);
        return rows.Select(r => (r.TaskType, r.ExternalId, r.MarketplaceClientId)).ToHashSet();
    }

    private async Task EnrichChatCardsFromHistoryAsync(List<CommunicationTaskDto> tasks, CancellationToken ct)
    {
        const int historyLimit = 1;
        var chats = tasks.Where(t => t.TaskType == CommunicationTaskType.Chat && !string.IsNullOrEmpty(t.ExternalId)).ToList();
        if (chats.Count == 0) return;

        using var sem = new SemaphoreSlim(8);
        async Task LoadOne(CommunicationTaskDto task)
        {
            await sem.WaitAsync(ct);
            try
            {
                var history = await _chatService.GetChatHistoryAsync(
                    task.MarketplaceClientId, task.ExternalId, "Backward", null, historyLimit, ct);
                var messages = history?.Messages;
                if (messages is not { Count: > 0 })
                {
                    task.LastMessageFromCustomer = false;
                    return;
                }

                var ordered = messages.OrderBy(m => m.CreatedAt).ToList();
                var last = ordered[^1];
                task.LastMessageFromCustomer = IsOzonChatMessageFromCustomer(last);
                task.PreviewText = BuildChatCardPreviewText(last);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "EnrichChatCardsFromHistory: chat {ChatId}", task.ExternalId);
                task.LastMessageFromCustomer = false;
            }
            finally
            {
                sem.Release();
            }
        }

        await Task.WhenAll(chats.Select(LoadOne));
    }

    private static string BuildChatCardPreviewText(OzonChatMessageDto last)
    {
        const int maxTotal = 220;

        var sender = GetChatSenderShortLabel(last);
        var body = ExtractChatMessageBodyPreview(last);
        if (string.IsNullOrWhiteSpace(body)) return "";

        var line = $"{sender}: {body}";
        if (line.Length <= maxTotal) return line;
        return line[..maxTotal].TrimEnd() + "...";
    }

    private static string GetChatSenderShortLabel(OzonChatMessageDto msg)
    {
        var t = msg.User?.Type;
        if (string.IsNullOrEmpty(t)) return "?";
        if (t is "Seller" or "seller" or "Seller_Support" or "SELLER_SUPPORT") return "Продавец";
        if (t is "Customer" or "Сustomer" or "customer" or "BUYER") return "Покупатель";
        if (t == "Support") return "Поддержка";
        return t;
    }

    private static string ExtractChatMessageBodyPreview(OzonChatMessageDto msg)
    {
        if (msg.IsImage) return "Изображение";

        if (msg.Data.Count == 0) return "";

        var chunks = new List<string>();
        foreach (var d in msg.Data)
        {
            if (string.IsNullOrWhiteSpace(d)) continue;
            if (d.Contains("/chat/file/", StringComparison.OrdinalIgnoreCase)
                || (d.Contains("api-seller.ozon.ru", StringComparison.OrdinalIgnoreCase) && d.Contains('[')))
            {
                chunks.Add("файл");
                continue;
            }

            var flat = d.Replace("\r\n", " ", StringComparison.Ordinal).Replace('\n', ' ').Trim();
            if (flat.Length > 80) flat = flat[..80].TrimEnd() + "…";
            chunks.Add(flat);
        }

        return string.Join(" ", chunks);
    }

    private static bool IsOzonChatMessageFromCustomer(OzonChatMessageDto msg)
    {
        var t = msg.User?.Type;
        if (string.IsNullOrEmpty(t)) return false;
        return IsOzonCustomerUserTypeString(t);
    }

    private static bool IsOzonCustomerUserTypeString(string t) =>
        t is "Customer" or "Сustomer" or "customer" or "BUYER";

    private static bool IsOzonCustomerLastMessageUserType(string? t)
    {
        if (string.IsNullOrEmpty(t)) return false;
        if (t is "Seller" or "seller" or "Seller_Support" or "SELLER_SUPPORT") return false;
        return IsOzonCustomerUserTypeString(t);
    }
}

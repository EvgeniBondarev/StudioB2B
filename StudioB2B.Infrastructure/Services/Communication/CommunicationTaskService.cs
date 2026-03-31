using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudioB2B.Domain.Constants;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Infrastructure.Persistence.Tenant;
using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Services.Communication;

public class CommunicationTaskService : ICommunicationTaskService
{
    private readonly TenantDbContext _db;
    private readonly ILogger<CommunicationTaskService> _logger;
    private readonly ICurrentUserProvider _currentUser;
    private readonly IOzonChatService _chatService;
    private readonly IOzonQuestionsService _questionsService;
    private readonly IOzonReviewsService _reviewsService;

    public CommunicationTaskService(
        TenantDbContext db,
        ILogger<CommunicationTaskService> logger,
        ICurrentUserProvider currentUser,
        IOzonChatService chatService,
        IOzonQuestionsService questionsService,
        IOzonReviewsService reviewsService)
    {
        _db = db;
        _logger = logger;
        _currentUser = currentUser;
        _chatService = chatService;
        _questionsService = questionsService;
        _reviewsService = reviewsService;
    }

    /// <summary>
    /// Ensures the user exists in the tenant Users table (required by FK constraints).
    /// Creates a stub record if missing.
    /// </summary>
    private async Task EnsureUserExistsAsync(Guid userId, CancellationToken ct)
    {
        var exists = await _db.Users.AnyAsync(u => u.Id == userId, ct);
        if (exists) return;

        var email = _currentUser.Email;
        var emailTaken = email is not null && await _db.Users.AnyAsync(u => u.Email == email, ct);

        _db.Users.Add(new TenantUser
        {
            Id = userId,
            Email = !emailTaken && email is not null ? email : $"{userId}@stub",
            FirstName = email?.Split('@').FirstOrDefault() ?? "User",
            LastName = "",
            HashPassword = "",
            IsActive = true
        });
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Created stub tenant user {UserId} for task board FK", userId);
    }

    public async Task<TaskBoardDto> GetBoardAsync(CommunicationTaskFilter filter, CancellationToken ct = default)
    {
        _db.ChangeTracker.Clear();

        var dbQuery = _db.CommunicationTasks
            .AsNoTracking()
            .Where(t => !t.IsDeleted &&
                        (t.TaskType != CommunicationTaskType.Chat ||
                         t.ChatType == null ||
                         t.ChatType == "BUYER_SELLER"));

        if (filter.TaskType.HasValue)
            dbQuery = dbQuery.Where(t => t.TaskType == filter.TaskType.Value);
        if (filter.AssignedToUserId.HasValue)
            dbQuery = dbQuery.Where(t => t.AssignedToUserId == filter.AssignedToUserId.Value);
        if (filter.MarketplaceClientId.HasValue)
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

        // New tasks come live from Ozon — not stored in DB.
        // Skip when filtering by specific user (live tasks are unassigned).
        var newItems = new List<CommunicationTaskDto>();
        if (!filter.AssignedToUserId.HasValue)
        {
            var inProgressSet = inProgressItems
                .Select(t => (t.TaskType, t.ExternalId, t.MarketplaceClientId))
                .ToHashSet();

            if (!filter.TaskType.HasValue || filter.TaskType == CommunicationTaskType.Chat)
                newItems.AddRange(await FetchLiveChatsAsync(filter.MarketplaceClientId, inProgressSet, filter.From, filter.To, ct));
            if (!filter.TaskType.HasValue || filter.TaskType == CommunicationTaskType.Question)
                newItems.AddRange(await FetchLiveQuestionsAsync(filter.MarketplaceClientId, inProgressSet, filter.From, filter.To, ct));
            if (!filter.TaskType.HasValue || filter.TaskType == CommunicationTaskType.Review)
                newItems.AddRange(await FetchLiveReviewsAsync(filter.MarketplaceClientId, inProgressSet, filter.From, filter.To, ct));
        }

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

        var activeRates = await _db.CommunicationPaymentRates
            .Include(r => r.User)
            .AsNoTracking()
            .Where(r => r.IsActive)
            .ToListAsync(ct);

        var globalRates = activeRates.Where(r => r.UserId == null).ToList();

        foreach (var type in new[] { CommunicationTaskType.Chat, CommunicationTaskType.Question, CommunicationTaskType.Review })
        {
            var matching = globalRates.Where(r => r.TaskType == null || r.TaskType == type).ToList();

            var perTask = matching
                .Where(r => r.PaymentMode == PaymentMode.PerTask && !r.MinDurationMinutes.HasValue)
                .Sum(r => r.Rate);
            if (perTask > 0) result.PaymentEstimates[type] = Math.Round(perTask, 2);

            var hourly = matching
                .Where(r => r.PaymentMode == PaymentMode.Hourly && !r.MinDurationMinutes.HasValue)
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

    public async Task<(List<CommunicationTaskDto> Items, int TotalCount)> GetDoneTasksPageAsync(
        CommunicationTaskFilter filter, int skip, int take, CancellationToken ct = default)
    {
        _db.ChangeTracker.Clear();

        var query = _db.CommunicationTasks
            .AsNoTracking()
            .Where(t => !t.IsDeleted &&
                        (t.TaskType != CommunicationTaskType.Chat ||
                         t.ChatType == null ||
                         t.ChatType == "BUYER_SELLER") &&
                        (t.Status == CommunicationTaskStatus.Done ||
                         t.Status == CommunicationTaskStatus.Cancelled));

        if (filter.TaskType.HasValue)
            query = query.Where(t => t.TaskType == filter.TaskType.Value);
        if (filter.AssignedToUserId.HasValue)
            query = query.Where(t => t.AssignedToUserId == filter.AssignedToUserId.Value);
        if (filter.MarketplaceClientId.HasValue)
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
            StartedAt = t.StartedAt,
            CompletedAt = t.CompletedAt,
            TotalTimeSpentTicks = t.TotalTimeSpentTicks,
            Title = t.Title,
            PreviewText = t.PreviewText,
            ExternalStatus = t.ExternalStatus,
            ChatType = t.ChatType,
            UnreadCount = t.UnreadCount,
            ExternalUrl = t.ExternalUrl,
            PaymentAmount = t.PaymentAmount,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt,
            HasActiveTimer = t.TimeEntries.Any(e => e.EndedAt == null)
        };
    }

    public async Task<CommunicationTaskDto?> FindActiveTaskAsync(string externalId, Guid marketplaceClientId, CancellationToken ct = default)
    {
        _db.ChangeTracker.Clear();

        return await _db.CommunicationTasks
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
        _db.ChangeTracker.Clear();

        return await _db.CommunicationTasks
            .AsNoTracking()
            .Where(t => !t.IsDeleted && t.Status == CommunicationTaskStatus.InProgress)
            .Select(ProjectToCardDto())
            .ToListAsync(ct);
    }

    public async Task<List<CommunicationTaskDto>> GetDoneTasksTodayAsync(CancellationToken ct = default)
    {
        _db.ChangeTracker.Clear();
        var todayUtc = DateTime.UtcNow.Date;

        return await _db.CommunicationTasks
            .AsNoTracking()
            .Where(t => !t.IsDeleted &&
                        t.Status == CommunicationTaskStatus.Done &&
                        t.CompletedAt >= todayUtc)
            .Select(ProjectToCardDto())
            .ToListAsync(ct);
    }

    public async Task<CommunicationTaskDetailDto?> GetTaskDetailAsync(Guid taskId, CancellationToken ct = default)
    {
        _db.ChangeTracker.Clear();

        var task = await _db.CommunicationTasks
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
        _db.ChangeTracker.Clear();
        _db.SuppressAudit = true;
        try
        {
            await EnsureUserExistsAsync(userId, ct);

            // Race guard: another user may have already claimed this Ozon item.
            var existing = await _db.CommunicationTasks
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
                    _db.CommunicationTimeEntries.Add(new CommunicationTimeEntry
                    {
                        Id = Guid.NewGuid(),
                        TaskId = existing.Id,
                        UserId = userId,
                        StartedAt = DateTime.UtcNow
                    });
                }

                await _db.SaveChangesAsync(ct);
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

            _db.CommunicationTasks.Add(task);

            _db.CommunicationTimeEntries.Add(new CommunicationTimeEntry
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

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Task {ExternalId} ({Type}) created and claimed by user {UserId}",
                liveTask.ExternalId, liveTask.TaskType, userId);
            return task.Id;
        }
        catch (Exception ex)
        {
            _db.ChangeTracker.Clear();
            _logger.LogError(ex, "CreateAndClaimAsync failed for {ExternalId}", liveTask.ExternalId);
            return null;
        }
        finally
        {
            _db.SuppressAudit = false;
        }
    }

    public async Task<bool> ClaimTaskAsync(Guid taskId, Guid userId, CancellationToken ct = default)
    {
        _db.ChangeTracker.Clear();
        _db.SuppressAudit = true;
        try
        {
            await EnsureUserExistsAsync(userId, ct);

            var task = await _db.CommunicationTasks
                .Include(t => t.TimeEntries)
                .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted, ct);

            if (task is null) return false;
            if (task.AssignedToUserId is not null && task.AssignedToUserId != userId) return false;

            task.AssignedToUserId = userId;
            task.AssignedAt = DateTime.UtcNow;
            task.Status = CommunicationTaskStatus.InProgress;
            task.StartedAt ??= DateTime.UtcNow;
            task.UpdatedAt = DateTime.UtcNow;

            _db.CommunicationTimeEntries.Add(new CommunicationTimeEntry
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                UserId = userId,
                StartedAt = DateTime.UtcNow
            });

            _db.CommunicationTaskLogs.Add(new CommunicationTaskLog
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                UserId = userId,
                Action = "Assigned",
                CreatedAt = DateTime.UtcNow
            });
            _db.CommunicationTaskLogs.Add(new CommunicationTaskLog
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                UserId = userId,
                Action = "Started",
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Task {TaskId} claimed by user {UserId}", taskId, userId);
            return true;
        }
        finally
        {
            _db.SuppressAudit = false;
        }
    }

    public async Task<bool> ReleaseTaskAsync(Guid taskId, Guid userId, CancellationToken ct = default)
    {
        _db.ChangeTracker.Clear();
        _db.SuppressAudit = true;
        try
        {
            var task = await _db.CommunicationTasks
                .Include(t => t.TimeEntries)
                .Include(t => t.Logs)
                .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted, ct);

            if (task is null || task.AssignedToUserId != userId) return false;

            // Delete the record so the task returns to the live Ozon feed.
            _db.CommunicationTasks.Remove(task);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Task {TaskId} released and removed from DB by user {UserId}", taskId, userId);
            return true;
        }
        finally
        {
            _db.SuppressAudit = false;
        }
    }

    public async Task<bool> PauseTaskAsync(Guid taskId, Guid userId, CancellationToken ct = default)
    {
        _db.ChangeTracker.Clear();
        _db.SuppressAudit = true;
        try
        {
            await EnsureUserExistsAsync(userId, ct);

            var task = await _db.CommunicationTasks
                .Include(t => t.TimeEntries)
                .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted, ct);

            if (task is null || task.AssignedToUserId != userId) return false;

            StopActiveTimers(task, userId);

            _db.CommunicationTaskLogs.Add(new CommunicationTaskLog
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                UserId = userId,
                Action = "Paused",
                CreatedAt = DateTime.UtcNow
            });

            task.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return true;
        }
        finally
        {
            _db.SuppressAudit = false;
        }
    }

    public async Task<bool> ResumeTaskAsync(Guid taskId, Guid userId, CancellationToken ct = default)
    {
        _db.ChangeTracker.Clear();
        _db.SuppressAudit = true;
        try
        {
            await EnsureUserExistsAsync(userId, ct);

            var task = await _db.CommunicationTasks
                .Include(t => t.TimeEntries)
                .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted, ct);

            if (task is null || task.AssignedToUserId != userId) return false;

            var hasActive = task.TimeEntries.Any(e => e.UserId == userId && e.EndedAt == null);
            if (hasActive) return true;

            _db.CommunicationTimeEntries.Add(new CommunicationTimeEntry
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                UserId = userId,
                StartedAt = DateTime.UtcNow
            });

            _db.CommunicationTaskLogs.Add(new CommunicationTaskLog
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                UserId = userId,
                Action = "Resumed",
                CreatedAt = DateTime.UtcNow
            });

            task.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return true;
        }
        finally
        {
            _db.SuppressAudit = false;
        }
    }

    public async Task<bool> CompleteTaskAsync(Guid taskId, Guid userId, CancellationToken ct = default)
    {
        _db.ChangeTracker.Clear();
        _db.SuppressAudit = true;
        try
        {
            await EnsureUserExistsAsync(userId, ct);

            var task = await _db.CommunicationTasks
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

            task.PaymentAmount = await CalculatePaymentAsync(task, ct);

            _db.CommunicationTaskLogs.Add(new CommunicationTaskLog
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                UserId = userId,
                Action = "Completed",
                Details = $"TotalTime: {TimeSpan.FromTicks(totalTicks):hh\\:mm\\:ss}, Payment: {task.PaymentAmount:F2}",
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Task {TaskId} completed by user {UserId}, payment={Payment}",
                taskId, userId, task.PaymentAmount);
            return true;
        }
        finally
        {
            _db.SuppressAudit = false;
        }
    }

    public async Task<PaymentReportDto> GetPaymentReportAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var doneTasks = await _db.CommunicationTasks
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
        var tasks = await _db.CommunicationTasks
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

        return new UserTaskDetailsDto { UserId = userId, UserName = userName, Tasks = items };
    }

    public async Task<PersonalStatsDto> GetPersonalStatsAsync(Guid userId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var tasks = await _db.CommunicationTasks
            .Include(t => t.MarketplaceClient)
            .Include(t => t.TimeEntries)
            .AsNoTracking()
            .Where(t => t.Status == CommunicationTaskStatus.Done &&
                        t.AssignedToUserId == userId &&
                        t.CompletedAt >= from && t.CompletedAt <= to &&
                        !t.IsDeleted)
            .OrderByDescending(t => t.CompletedAt)
            .ToListAsync(ct);

        var user = await _db.Users.AsNoTracking()
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
        return await _db.CommunicationPaymentRates
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
        _db.ChangeTracker.Clear();
        _db.SuppressAudit = true;
        try
        {
            CommunicationPaymentRate entity;

            if (dto.Id == Guid.Empty)
            {
                entity = new CommunicationPaymentRate { Id = Guid.NewGuid() };
                _db.CommunicationPaymentRates.Add(entity);
            }
            else
            {
                entity = await _db.CommunicationPaymentRates.FindAsync(new object[] { dto.Id }, ct)
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

            await _db.SaveChangesAsync(ct);

            dto.Id = entity.Id;
            return dto;
        }
        finally
        {
            _db.SuppressAudit = false;
        }
    }

    public async Task<bool> DeletePaymentRateAsync(Guid id, CancellationToken ct = default)
    {
        _db.ChangeTracker.Clear();

        var entity = await _db.CommunicationPaymentRates.FindAsync(new object[] { id }, ct);
        if (entity is null) return false;

        _db.CommunicationPaymentRates.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<int> RecalculatePaymentsAsync(CancellationToken ct = default)
    {
        _db.ChangeTracker.Clear();
        _db.SuppressAudit = true;
        try
        {
            var done = await _db.CommunicationTasks
                .Include(t => t.TimeEntries)
                .Where(t => t.Status == CommunicationTaskStatus.Done && !t.IsDeleted)
                .ToListAsync(ct);

            foreach (var task in done)
            {
                task.PaymentAmount = await CalculatePaymentAsync(task, ct);
                task.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("RecalculatePayments: updated {Count} tasks", done.Count);
            return done.Count;
        }
        finally
        {
            _db.SuppressAudit = false;
        }
    }

    /// <summary>
    /// Sums ALL matching active rates for the completed task.
    /// A rate matches when: (TaskType is null OR == task.TaskType) AND (UserId is null OR == task.AssignedToUserId)
    /// AND task duration falls within [MinDurationMinutes, MaxDurationMinutes] (inclusive, null = unbounded).
    /// Hourly rates are multiplied by time spent; PerTask rates are added as-is.
    /// All matching rules are applied simultaneously — e.g. a base 50₽ rule + a conditional 40₽ rule both apply.
    /// </summary>
    private async Task<decimal> CalculatePaymentAsync(CommunicationTask task, CancellationToken ct)
    {
        // Extract to local variables so EF Core doesn't try to translate
        // entity property access inside the query expression tree.
        var taskType = task.TaskType;
        var assignedUserId = task.AssignedToUserId;
        var totalMinutes = (decimal)TimeSpan.FromTicks(task.TotalTimeSpentTicks).TotalMinutes;

        var rates = await _db.CommunicationPaymentRates
            .AsNoTracking()
            .Where(r => r.IsActive &&
                        (r.TaskType == null || r.TaskType == taskType) &&
                        (r.UserId == null || r.UserId == assignedUserId))
            .ToListAsync(ct);

        if (rates.Count == 0) return 0m;

        var total = 0m;

        foreach (var rate in rates)
        {
            // Each rule is evaluated independently — skip only if duration is outside this rule's range.
            if (rate.MinDurationMinutes.HasValue && totalMinutes < rate.MinDurationMinutes.Value)
                continue;
            if (rate.MaxDurationMinutes.HasValue && totalMinutes > rate.MaxDurationMinutes.Value)
                continue;

            total += rate.PaymentMode switch
            {
                PaymentMode.PerTask => rate.Rate,
                PaymentMode.Hourly => rate.Rate * (totalMinutes / 60m),
                _ => 0m
            };
        }

        return Math.Round(total, 2);
    }

    private async Task<List<CommunicationTaskDto>> FetchLiveChatsAsync(
        Guid? marketplaceClientId,
        HashSet<(CommunicationTaskType, string, Guid)> inProgressSet,
        DateTime? from, DateTime? to,
        CancellationToken ct)
    {
        try
        {
            // Fan out per client so every client's chats are queried (not just the first one
            // filling up the shared page-size quota).
            var clientIds = marketplaceClientId.HasValue
                ? [marketplaceClientId.Value]
                : await _db.MarketplaceClients.AsNoTracking()
                    .Where(c => !c.IsDeleted)
                    .Select(c => c.Id)
                    .ToArrayAsync(ct);

            var pageTasks = clientIds.Select(id => _chatService.GetChatsPageAsync(
                pageSize: 50, chatStatus: "OPENED", chatType: "BUYER_SELLER",
                marketplaceClientId: id, ct: ct));
            var pages = await Task.WhenAll(pageTasks);

            var toExclusive = to.HasValue ? to.Value.Date.AddDays(1) : DateTime.MaxValue;

            var result = new List<CommunicationTaskDto>();
            foreach (var chat in pages.SelectMany(p => p.Chats))
            {
                if (inProgressSet.Contains((CommunicationTaskType.Chat, chat.ChatId, chat.MarketplaceClientId)))
                    continue;
                if (from.HasValue && chat.LastMessageAt < from.Value)
                    continue;
                if (chat.LastMessageAt >= toExclusive)
                    continue;
                // Only show chats where the last message is from the buyer (seller hasn't replied yet).
                if (chat.LastMessageUserType is not null &&
                    (chat.LastMessageUserType.Equals("Seller", StringComparison.OrdinalIgnoreCase) ||
                     chat.LastMessageUserType.Equals("Seller_Support", StringComparison.OrdinalIgnoreCase) ||
                     chat.LastMessageUserType.Equals("SELLER_SUPPORT", StringComparison.OrdinalIgnoreCase)))
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
                    UpdatedAt = DateTime.UtcNow
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

    private async Task<List<CommunicationTaskDto>> FetchLiveQuestionsAsync(
        Guid? marketplaceClientId,
        HashSet<(CommunicationTaskType, string, Guid)> inProgressSet,
        DateTime? from, DateTime? to,
        CancellationToken ct)
    {
        try
        {
            var toExclusive = to.HasValue ? (DateTime?)to.Value.Date.AddDays(1) : null;

            // Fan out per client.
            var clientIds = marketplaceClientId.HasValue
                ? [marketplaceClientId.Value]
                : await _db.MarketplaceClients.AsNoTracking()
                    .Where(c => !c.IsDeleted)
                    .Select(c => c.Id)
                    .ToArrayAsync(ct);

            var pageTasks = clientIds.Select(id => _questionsService.GetQuestionsPageAsync(
                pageSize: 50, cursor: null, dateFrom: from, dateTo: toExclusive,
                marketplaceClientId: id, ct: ct));
            var pages = await Task.WhenAll(pageTasks);

            var result = new List<CommunicationTaskDto>();
            foreach (var q in pages.SelectMany(p => p.Questions))
            {
                if (string.Equals(q.Status, "PROCESSED", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (inProgressSet.Contains((CommunicationTaskType.Question, q.Id, q.MarketplaceClientId)))
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
        HashSet<(CommunicationTaskType, string, Guid)> inProgressSet,
        DateTime? from, DateTime? to,
        CancellationToken ct)
    {
        try
        {
            // Fan out per client.
            var clientIds = marketplaceClientId.HasValue
                ? [marketplaceClientId.Value]
                : await _db.MarketplaceClients.AsNoTracking()
                    .Where(c => !c.IsDeleted)
                    .Select(c => c.Id)
                    .ToArrayAsync(ct);

            var pageTasks = clientIds.Select(id => _reviewsService.GetReviewsPageAsync(
                pageSize: 50, cursor: null, marketplaceClientId: id, ct: ct));
            var pages = await Task.WhenAll(pageTasks);

            var toExclusive = to.HasValue ? to.Value.Date.AddDays(1) : DateTime.MaxValue;

            var result = new List<CommunicationTaskDto>();
            foreach (var r in pages.SelectMany(p => p.Reviews))
            {
                if (string.Equals(r.Status, "PROCESSED", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (inProgressSet.Contains((CommunicationTaskType.Review, r.Id, r.MarketplaceClientId)))
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
        dto.StartedAt = t.StartedAt;
        dto.CompletedAt = t.CompletedAt;
        dto.TotalTimeSpentTicks = t.TotalTimeSpentTicks;
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
    }
}

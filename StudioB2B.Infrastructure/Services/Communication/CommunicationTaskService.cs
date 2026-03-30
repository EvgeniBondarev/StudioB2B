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

    public CommunicationTaskService(TenantDbContext db, ILogger<CommunicationTaskService> logger,
        ICurrentUserProvider currentUser)
    {
        _db = db;
        _logger = logger;
        _currentUser = currentUser;
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

        // Base predicate (no entity loads, no joins via Include)
        var baseQuery = _db.CommunicationTasks
            .AsNoTracking()
            .Where(t => !t.IsDeleted &&
                        (t.TaskType != CommunicationTaskType.Chat ||
                         t.ChatType == null ||
                         t.ChatType == "BUYER_SELLER"));

        if (filter.TaskType.HasValue)
            baseQuery = baseQuery.Where(t => t.TaskType == filter.TaskType.Value);
        if (filter.AssignedToUserId.HasValue)
            baseQuery = baseQuery.Where(t => t.AssignedToUserId == filter.AssignedToUserId.Value);
        if (filter.MarketplaceClientId.HasValue)
            baseQuery = baseQuery.Where(t => t.MarketplaceClientId == filter.MarketplaceClientId.Value);

        // Projection: only the columns the board cards actually need.
        // HasActiveTimer uses a correlated EXISTS — no TimeEntries rows transferred.
        var activeItems = await baseQuery
            .Where(t => t.Status == CommunicationTaskStatus.New ||
                        t.Status == CommunicationTaskStatus.InProgress)
            .OrderByDescending(t => t.CreatedAt)
            .Select(ProjectToCardDto())
            .ToListAsync(ct);

        var doneQuery = baseQuery
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

        // TypeCounts: a single GROUP BY COUNT — no row transfer
        var typeCounts = await baseQuery
            .GroupBy(t => t.TaskType)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var result = new TaskBoardDto
        {
            DoneTotalCount = doneTotalCount,
            TypeCounts = typeCounts.ToDictionary(x => x.Key, x => x.Count)
        };

        foreach (var dto in activeItems)
        {
            if (dto.Status == CommunicationTaskStatus.New)
                result.NewTasks.Add(dto);
            else
                result.InProgressTasks.Add(dto);
        }

        result.DoneTasks.AddRange(doneItems);

        // Payment estimates from active global (non-user-specific) rates
        var activeRates = await _db.CommunicationPaymentRates
            .AsNoTracking()
            .Where(r => r.IsActive && r.UserId == null)
            .ToListAsync(ct);

        foreach (var type in new[] { CommunicationTaskType.Chat, CommunicationTaskType.Question, CommunicationTaskType.Review })
        {
            var matching = activeRates.Where(r => r.TaskType == null || r.TaskType == type).ToList();

            var perTask = matching
                .Where(r => r.PaymentMode == PaymentMode.PerTask && !r.MinDurationMinutes.HasValue)
                .Sum(r => r.Rate);
            if (perTask > 0) result.PaymentEstimates[type] = Math.Round(perTask, 2);

            var hourly = matching
                .Where(r => r.PaymentMode == PaymentMode.Hourly && !r.MinDurationMinutes.HasValue)
                .Sum(r => r.Rate);
            if (hourly > 0) result.HourlyEstimates[type] = Math.Round(hourly, 2);
        }

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
            await EnsureUserExistsAsync(userId, ct);

            var task = await _db.CommunicationTasks
                .Include(t => t.TimeEntries)
                .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted, ct);

            if (task is null || task.AssignedToUserId != userId) return false;

            StopActiveTimers(task, userId);

            task.AssignedToUserId = null;
            task.AssignedAt = null;
            task.Status = CommunicationTaskStatus.New;
            task.UpdatedAt = DateTime.UtcNow;

            _db.CommunicationTaskLogs.Add(new CommunicationTaskLog
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                UserId = userId,
                Action = "Released",
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(ct);
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
    /// </summary>
    private async Task<decimal> CalculatePaymentAsync(CommunicationTask task, CancellationToken ct)
    {
        var rates = await _db.CommunicationPaymentRates
            .AsNoTracking()
            .Where(r => r.IsActive &&
                        (r.TaskType == null || r.TaskType == task.TaskType) &&
                        (r.UserId == null || r.UserId == task.AssignedToUserId))
            .ToListAsync(ct);

        if (rates.Count == 0) return 0m;

        var totalMinutes = (decimal)TimeSpan.FromTicks(task.TotalTimeSpentTicks).TotalMinutes;
        var total = 0m;

        foreach (var rate in rates)
        {
            // Skip if duration is outside the configured range
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

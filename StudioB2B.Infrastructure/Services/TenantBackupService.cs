using Hangfire;
using Hangfire.States;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using StudioB2B.Domain.Entities;
using StudioB2B.Domain.Options;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Infrastructure.Persistence.Master;
using StudioB2B.Infrastructure.Services.MultiTenancy;
using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Services;

public class TenantBackupService : ITenantBackupService
{
    private readonly MasterDbContext _masterDb;
    private readonly MasterHangfireManager _hangfireManager;
    private readonly IMinioClient _minio;
    private readonly BackupOptions _options;

    public TenantBackupService(
        MasterDbContext masterDb,
        MasterHangfireManager hangfireManager,
        IMinioClient minio,
        IOptions<BackupOptions> options)
    {
        _masterDb = masterDb;
        _hangfireManager = hangfireManager;
        _minio = minio;
        _options = options.Value;
    }

    public async Task<TenantBackupScheduleDto?> GetScheduleAsync(Guid tenantId, CancellationToken ct = default)
    {
        var schedule = await _masterDb.TenantBackupSchedules
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId, ct);

        return schedule is null ? null : ToDto(schedule);
    }

    public async Task<TenantBackupScheduleDto> SaveScheduleAsync(SaveTenantBackupScheduleDto dto, CancellationToken ct = default)
    {
        var tenant = await _masterDb.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == dto.TenantId, ct)
            ?? throw new InvalidOperationException($"Tenant {dto.TenantId} not found.");

        var schedule = await _masterDb.TenantBackupSchedules
            .FirstOrDefaultAsync(s => s.TenantId == dto.TenantId, ct);

        if (schedule is null)
        {
            schedule = new TenantBackupSchedule
            {
                TenantId = dto.TenantId,
                HangfireJobId = $"tenant-backup-{dto.TenantId:N}"
            };
            _masterDb.TenantBackupSchedules.Add(schedule);
        }

        schedule.IsEnabled = dto.IsEnabled;
        schedule.CronExpression = dto.CronExpression;
        schedule.RetentionDays = dto.RetentionDays;
        schedule.UpdatedAtUtc = DateTime.UtcNow;

        await _masterDb.SaveChangesAsync(ct);

        var manager = _hangfireManager.RecurringJobManager;

        if (dto.IsEnabled)
        {
            var job = Hangfire.Common.Job.FromExpression<TenantBackupJob>(
                j => j.ExecuteAsync(dto.TenantId, tenant.ConnectionString, tenant.Subdomain, CancellationToken.None));

            manager.AddOrUpdate(schedule.HangfireJobId!, job, dto.CronExpression, new RecurringJobOptions());
        }
        else
        {
            manager.RemoveIfExists(schedule.HangfireJobId!);
        }

        return ToDto(schedule);
    }

    public async Task DeleteScheduleAsync(Guid tenantId, CancellationToken ct = default)
    {
        var schedule = await _masterDb.TenantBackupSchedules
            .FirstOrDefaultAsync(s => s.TenantId == tenantId, ct);

        if (schedule is null) return;

        if (schedule.HangfireJobId is not null)
            _hangfireManager.RecurringJobManager.RemoveIfExists(schedule.HangfireJobId);

        _masterDb.TenantBackupSchedules.Remove(schedule);
        await _masterDb.SaveChangesAsync(ct);
    }

    public async Task TriggerBackupNowAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await _masterDb.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct)
            ?? throw new InvalidOperationException($"Tenant {tenantId} not found.");

        _hangfireManager.Client.Create<TenantBackupJob>(
            j => j.ExecuteAsync(tenantId, tenant.ConnectionString, tenant.Subdomain, CancellationToken.None),
            new EnqueuedState("master-backup"));
    }

    public async Task<List<TenantBackupHistoryDto>> GetHistoryAsync(Guid tenantId, int limit = 10, CancellationToken ct = default)
    {
        var records = await _masterDb.TenantBackupHistories
            .AsNoTracking()
            .Where(h => h.TenantId == tenantId)
            .OrderByDescending(h => h.StartedAtUtc)
            .Take(limit)
            .ToListAsync(ct);

        return records.Select(ToDto).ToList();
    }

    public async Task<string> GetDownloadUrlAsync(Guid historyId, CancellationToken ct = default)
    {
        var history = await _masterDb.TenantBackupHistories
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == historyId, ct)
            ?? throw new InvalidOperationException($"Backup history {historyId} not found.");

        if (history.MinioObjectKey is null)
            throw new InvalidOperationException("Backup file is not available.");

        var rawEndpoint = string.IsNullOrWhiteSpace(_options.PublicEndpoint)
            ? _options.Endpoint
            : _options.PublicEndpoint;

        // If the endpoint contains an explicit scheme, derive SSL from it.
        // Otherwise fall back to the UseSSL option.
        bool useSSL;
        string endpoint;
        if (rawEndpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            useSSL = true;
            endpoint = rawEndpoint["https://".Length..];
        }
        else if (rawEndpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            useSSL = false;
            endpoint = rawEndpoint["http://".Length..];
        }
        else
        {
            useSSL = _options.UseSSL;
            endpoint = rawEndpoint;
        }

        var presignedClient = new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(_options.AccessKey, _options.SecretKey)
            .WithSSL(useSSL)
            .Build();

        var presignedArgs = new PresignedGetObjectArgs()
            .WithBucket(_options.Bucket)
            .WithObject(history.MinioObjectKey)
            .WithExpiry(900);

        return await presignedClient.PresignedGetObjectAsync(presignedArgs);
    }

    private static TenantBackupScheduleDto ToDto(TenantBackupSchedule s) => new()
    {
        Id = s.Id,
        TenantId = s.TenantId,
        IsEnabled = s.IsEnabled,
        CronExpression = s.CronExpression,
        RetentionDays = s.RetentionDays,
        HangfireJobId = s.HangfireJobId,
        UpdatedAtUtc = s.UpdatedAtUtc
    };

    private static TenantBackupHistoryDto ToDto(TenantBackupHistory h) => new()
    {
        Id = h.Id,
        TenantId = h.TenantId,
        MinioObjectKey = h.MinioObjectKey,
        SizeBytes = h.SizeBytes,
        Status = h.Status,
        ErrorMessage = h.ErrorMessage,
        StartedAtUtc = h.StartedAtUtc,
        CompletedAtUtc = h.CompletedAtUtc
    };
}


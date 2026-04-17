using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Domain.Constants;
using StudioB2B.Domain.Entities;
using Xunit;

namespace StudioB2B.Tests.Integration.Database;

[Collection("Database")]
public class SyncJobTests : IClassFixture<TenantDbContextFixture>
{
    private readonly TenantDbContextFixture _fixture;

    public SyncJobTests(TenantDbContextFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task CreateSyncJobSchedule_Persists()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var schedule = new SyncJobSchedule
        {
            Id = Guid.NewGuid(),
            JobType = SyncJobTypeEnum.Sync,
            IsEnabled = true,
            CronExpression = "0 9 * * *",
            CronDescription = "Every day at 09:00"
        };
        ctx.SyncJobSchedules.Add(schedule);
        await ctx.SaveChangesAsync();

        var loaded = await ctx.SyncJobSchedules.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == schedule.Id);
        loaded.Should().NotBeNull();
        loaded!.CronExpression.Should().Be("0 9 * * *");
        loaded.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateSyncJobSchedule_CronExpression_Persists()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var schedule = new SyncJobSchedule
        {
            Id = Guid.NewGuid(),
            JobType = SyncJobTypeEnum.Sync,
            CronExpression = "0 9 * * *"
        };
        ctx.SyncJobSchedules.Add(schedule);
        await ctx.SaveChangesAsync();

        var entity = await ctx.SyncJobSchedules.FindAsync(schedule.Id);
        entity!.CronExpression = "0 18 * * *";
        entity.IsEnabled = false;
        await ctx.SaveChangesAsync();

        var loaded = await ctx.SyncJobSchedules.AsNoTracking()
            .FirstAsync(s => s.Id == schedule.Id);
        loaded.CronExpression.Should().Be("0 18 * * *");
        loaded.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task CreateSyncJobHistory_Persists()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var history = new SyncJobHistory
        {
            Id = Guid.NewGuid(),
            HangfireJobId = $"job-{Guid.NewGuid():N}",
            JobType = SyncJobTypeEnum.Sync,
            Status = SyncJobStatusEnum.Enqueued,
            StartedAtUtc = DateTime.UtcNow
        };
        ctx.SyncJobHistories.Add(history);
        await ctx.SaveChangesAsync();

        var loaded = await ctx.SyncJobHistories.AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == history.Id);
        loaded.Should().NotBeNull();
        loaded!.Status.Should().Be(SyncJobStatusEnum.Enqueued);
    }

    [Fact]
    public async Task UpdateSyncJobHistory_Status_Completed()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var history = new SyncJobHistory
        {
            Id = Guid.NewGuid(),
            HangfireJobId = $"job-{Guid.NewGuid():N}",
            JobType = SyncJobTypeEnum.Sync,
            Status = SyncJobStatusEnum.Enqueued,
            StartedAtUtc = DateTime.UtcNow
        };
        ctx.SyncJobHistories.Add(history);
        await ctx.SaveChangesAsync();

        var entity = await ctx.SyncJobHistories.FindAsync(history.Id);
        entity!.Status = SyncJobStatusEnum.Succeeded;
        entity.FinishedAtUtc = DateTime.UtcNow;
        entity.ResultJson = "{\"created\":5}";
        await ctx.SaveChangesAsync();

        var loaded = await ctx.SyncJobHistories.AsNoTracking()
            .FirstAsync(h => h.Id == history.Id);
        loaded.Status.Should().Be(SyncJobStatusEnum.Succeeded);
        loaded.FinishedAtUtc.Should().NotBeNull();
        loaded.ResultJson.Should().Contain("created");
    }

    [Fact]
    public async Task FilterSyncJobHistories_ByJobType()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var syncJob = new SyncJobHistory { Id = Guid.NewGuid(), HangfireJobId = $"j-{Guid.NewGuid():N}", JobType = SyncJobTypeEnum.Sync, StartedAtUtc = DateTime.UtcNow };
        var updateJob = new SyncJobHistory { Id = Guid.NewGuid(), HangfireJobId = $"j-{Guid.NewGuid():N}", JobType = SyncJobTypeEnum.Update, StartedAtUtc = DateTime.UtcNow };
        ctx.SyncJobHistories.AddRange(syncJob, updateJob);
        await ctx.SaveChangesAsync();

        var syncJobs = await ctx.SyncJobHistories.AsNoTracking()
            .Where(h => h.JobType == SyncJobTypeEnum.Sync && (h.Id == syncJob.Id || h.Id == updateJob.Id))
            .ToListAsync();

        syncJobs.Should().HaveCount(1);
        syncJobs[0].Id.Should().Be(syncJob.Id);
    }
}

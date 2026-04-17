using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Domain.Constants;
using Xunit;

namespace StudioB2B.Tests.Integration.Database;

[Collection("Database")]
public class CommunicationTaskCrudTests : IClassFixture<TenantDbContextFixture>
{
    private readonly TenantDbContextFixture _fixture;

    public CommunicationTaskCrudTests(TenantDbContextFixture fixture)
    {
        _fixture = fixture;
        _fixture.SeedReferenceDataAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task CreateCommunicationTask_Persists()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var task = DatabaseSeeder.CommunicationTask(_fixture.DefaultClientId);
        ctx.CommunicationTasks.Add(task);
        await ctx.SaveChangesAsync();

        var loaded = await ctx.CommunicationTasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == task.Id);
        loaded.Should().NotBeNull();
        loaded!.MarketplaceClientId.Should().Be(_fixture.DefaultClientId);
        loaded.TaskType.Should().Be(CommunicationTaskType.Chat);
        loaded.Status.Should().Be(CommunicationTaskStatus.New);
    }

    [Fact]
    public async Task AddTaskLog_NavigationLoadsCorrectly()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var task = DatabaseSeeder.CommunicationTask(_fixture.DefaultClientId);
        ctx.CommunicationTasks.Add(task);
        await ctx.SaveChangesAsync();

        var log = DatabaseSeeder.TaskLog(task.Id, "Assigned");
        ctx.CommunicationTaskLogs.Add(log);
        await ctx.SaveChangesAsync();

        var loaded = await ctx.CommunicationTasks
            .Include(t => t.Logs)
            .AsNoTracking()
            .FirstAsync(t => t.Id == task.Id);

        loaded.Logs.Should().HaveCount(1);
        loaded.Logs.First().Action.Should().Be("Assigned");
    }

    [Fact]
    public async Task SoftDeleteTask_NotReturnedByDefaultQuery()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var task = DatabaseSeeder.CommunicationTask(_fixture.DefaultClientId);
        ctx.CommunicationTasks.Add(task);
        await ctx.SaveChangesAsync();

        task.IsDeleted = true;
        ctx.CommunicationTasks.Update(task);
        await ctx.SaveChangesAsync();

        var found = await ctx.CommunicationTasks.AsNoTracking().AnyAsync(t => t.Id == task.Id);
        found.Should().BeFalse("soft-deleted task must be excluded by query filter");

        var foundIgnored = await ctx.CommunicationTasks
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(t => t.Id == task.Id);
        foundIgnored.Should().BeTrue("soft-deleted task must exist with IgnoreQueryFilters");
    }

    [Fact]
    public async Task UpdateTaskStatus_Persists()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var task = DatabaseSeeder.CommunicationTask(_fixture.DefaultClientId);
        ctx.CommunicationTasks.Add(task);
        await ctx.SaveChangesAsync();

        var entity = await ctx.CommunicationTasks.FindAsync(task.Id);
        entity!.Status = CommunicationTaskStatus.Done;
        entity.CompletedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync();

        var loaded = await ctx.CommunicationTasks.AsNoTracking().FirstAsync(t => t.Id == task.Id);
        loaded.Status.Should().Be(CommunicationTaskStatus.Done);
        loaded.CompletedAt.Should().NotBeNull();
    }
}


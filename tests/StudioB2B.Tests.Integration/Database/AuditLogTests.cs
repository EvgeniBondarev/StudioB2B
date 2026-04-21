using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Persistence.Tenant;
using StudioB2B.Infrastructure.Services.MultiTenancy;
using Xunit;

namespace StudioB2B.Tests.Integration.Database;

[Collection("Database")]
public class AuditLogTests : IClassFixture<TenantDbContextFixture>
{
    private readonly TenantDbContextFixture _fixture;

    public AuditLogTests(TenantDbContextFixture fixture) => _fixture = fixture;

    private static async Task SeedAsync(TenantDbContext ctx)
    {
        var method = typeof(TenantDatabaseInitializer)
            .GetMethod("SeedPagesColumnsAndFunctionsAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var task = (Task)method!.Invoke(null, [ctx, CancellationToken.None])!;
        await task;
    }

    [Fact]
    public async Task SaveChanges_OnAdd_CreatesAuditLog()
    {
        await using var ctx = _fixture.CreateContext();
        await SeedAsync(ctx);

        ctx.SuppressAudit = false;

        var status = new OrderStatus
        {
            Id = Guid.NewGuid(),
            Name = "AuditTestStatus",
            IsInternal = false,
            IsTerminal = false
        };
        ctx.OrderStatuses.Add(status);
        await ctx.SaveChangesAsync();

        var logs = await ctx.FieldAuditLogs.AsNoTracking()
            .Where(l => l.EntityId == status.Id.ToString())
            .ToListAsync();

        logs.Should().NotBeEmpty("adding an entity should generate FieldAuditLog entries");
        logs.Should().Contain(l => l.ChangeType == "Added");
    }

    [Fact]
    public async Task SaveChanges_WithSuppressAudit_NoLogsCreated()
    {
        await using var ctx = _fixture.CreateContext();
        await SeedAsync(ctx);

        ctx.SuppressAudit = true;

        var statusId = Guid.NewGuid();
        ctx.OrderStatuses.Add(new OrderStatus
        {
            Id = statusId,
            Name = "NoAuditStatus",
            IsInternal = false,
            IsTerminal = false
        });
        await ctx.SaveChangesAsync();

        var count = await ctx.FieldAuditLogs.AsNoTracking()
            .CountAsync(l => l.EntityId == statusId.ToString());

        count.Should().Be(0, "SuppressAudit=true must prevent audit log creation");
    }
}

using FluentAssertions;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Features;
using StudioB2B.Shared;
using Xunit;

namespace StudioB2B.Tests.Integration.Database;

/// <summary>
/// Проверяет запросы лога аудита: пагинация с фильтрами,
/// выборка по сущности, получение списка имён сущностей.
/// </summary>
[Collection("Database")]
public class AuditLogQueryTests : IClassFixture<TenantDbContextFixture>
{
    private readonly TenantDbContextFixture _fixture;

    public AuditLogQueryTests(TenantDbContextFixture fixture) => _fixture = fixture;

    private static FieldAuditLog NewLog(string entityName, string changeType, string? user = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            EntityName = entityName,
            EntityId = Guid.NewGuid().ToString(),
            FieldName = "Name",
            ChangeType = changeType,
            ChangedByUserName = user,
            ChangedAtUtc = DateTime.UtcNow
        };

    [Fact]
    public async Task GetAuditPaged_FilterByEntityName_ReturnsOnlyMatching()
    {
        await using var ctx = _fixture.CreateContext();

        var tag = Guid.NewGuid().ToString("N")[..8];
        var targetEntity = $"Entity_{tag}";
        ctx.FieldAuditLogs.AddRange(
            NewLog(targetEntity, "Modified"),
            NewLog(targetEntity, "Modified"),
            NewLog($"Other_{tag}", "Modified"));
        await ctx.SaveChangesAsync();

        var filter = new AuditLogFilter(EntityName: targetEntity);
        var (items, total) = await ctx.GetAuditPagedAsync(filter, 0, 100, null);

        items.Should().OnlyContain(x => x.EntityName == targetEntity);
        total.Should().Be(2);
    }

    [Fact]
    public async Task GetAuditPaged_FilterByChangeType_ReturnsOnlyMatching()
    {
        await using var ctx = _fixture.CreateContext();

        var tag = Guid.NewGuid().ToString("N")[..8];
        ctx.FieldAuditLogs.AddRange(
            NewLog($"E_{tag}", "Added"),
            NewLog($"E_{tag}", "Modified"),
            NewLog($"E_{tag}", "Added"));
        await ctx.SaveChangesAsync();

        var filter = new AuditLogFilter(EntityName: $"E_{tag}", ChangeType: "Added");
        var (items, total) = await ctx.GetAuditPagedAsync(filter, 0, 100, null);

        items.Should().OnlyContain(x => x.ChangeType == "Added");
        total.Should().Be(2);
    }

    [Fact]
    public async Task GetAuditPaged_FilterByDateRange_ReturnsOnlyInRange()
    {
        await using var ctx = _fixture.CreateContext();

        var tag = Guid.NewGuid().ToString("N")[..8];
        var entityName = $"Dated_{tag}";
        var now = DateTime.UtcNow;

        var old = NewLog(entityName, "Modified");
        old.ChangedAtUtc = now.AddDays(-10);
        var recent = NewLog(entityName, "Modified");
        recent.ChangedAtUtc = now.AddDays(-1);

        ctx.FieldAuditLogs.AddRange(old, recent);
        await ctx.SaveChangesAsync();

        var filter = new AuditLogFilter(EntityName: entityName, FromUtc: now.AddDays(-3));
        var (items, total) = await ctx.GetAuditPagedAsync(filter, 0, 100, null);

        items.Should().Contain(x => x.Id == recent.Id);
        items.Should().NotContain(x => x.Id == old.Id);
        total.Should().Be(1);
    }

    [Fact]
    public async Task GetAuditByEntity_ReturnsAllLogsForEntity()
    {
        await using var ctx = _fixture.CreateContext();

        var entityName = $"Spec_{Guid.NewGuid():N}";
        var entityId = Guid.NewGuid().ToString();

        ctx.FieldAuditLogs.AddRange(
            new FieldAuditLog { Id = Guid.NewGuid(), EntityName = entityName, EntityId = entityId, FieldName = "X", ChangeType = "Added", ChangedAtUtc = DateTime.UtcNow },
            new FieldAuditLog { Id = Guid.NewGuid(), EntityName = entityName, EntityId = entityId, FieldName = "Y", ChangeType = "Modified", ChangedAtUtc = DateTime.UtcNow },
            new FieldAuditLog { Id = Guid.NewGuid(), EntityName = entityName, EntityId = Guid.NewGuid().ToString(), FieldName = "Z", ChangeType = "Modified", ChangedAtUtc = DateTime.UtcNow });
        await ctx.SaveChangesAsync();

        var logs = await ctx.GetAuditByEntityAsync(entityName, entityId);

        logs.Should().HaveCount(2);
        logs.Should().OnlyContain(x => x.EntityId == entityId);
    }

    [Fact]
    public async Task GetAuditEntityNames_ContainsInsertedEntityName()
    {
        await using var ctx = _fixture.CreateContext();

        var uniqueEntity = $"Unique_{Guid.NewGuid():N}";
        ctx.FieldAuditLogs.Add(NewLog(uniqueEntity, "Added"));
        await ctx.SaveChangesAsync();

        var names = await ctx.GetAuditEntityNamesAsync();

        names.Should().Contain(uniqueEntity);
    }
}

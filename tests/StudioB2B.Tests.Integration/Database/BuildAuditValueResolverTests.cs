using FluentAssertions;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Features;
using System.Text.Json;
using Xunit;

namespace StudioB2B.Tests.Integration.Database;

[Collection("Database")]
public class BuildAuditValueResolverTests : IClassFixture<TenantDbContextFixture>
{
    private readonly TenantDbContextFixture _fixture;

    public BuildAuditValueResolverTests(TenantDbContextFixture fixture)
    {
        _fixture = fixture;
        _fixture.SeedReferenceDataAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task BuildAuditValueResolver_ResolvesOrderStatusGuid()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var statusId = _fixture.DefaultFromStatusId;
        var log = new FieldAuditLog
        {
            Id = Guid.NewGuid(),
            EntityName = "OrderEntity",
            EntityId = Guid.NewGuid().ToString(),
            FieldName = "StatusId",
            ChangeType = "Modified",
            NewValue = JsonSerializer.Serialize(statusId),
            ChangedAtUtc = DateTime.UtcNow
        };

        var resolver = await ctx.BuildAuditValueResolverAsync(new List<FieldAuditLog> { log });

        resolver.Should().ContainKey(statusId.ToString());
        resolver[statusId.ToString()].Should().Be("Test-From");
    }

    [Fact]
    public async Task BuildAuditValueResolver_ResolvesMarketplaceClientGuid()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var clientId = _fixture.DefaultClientId;
        var log = new FieldAuditLog
        {
            Id = Guid.NewGuid(),
            EntityName = "Shipment",
            EntityId = Guid.NewGuid().ToString(),
            FieldName = "MarketplaceClientId",
            ChangeType = "Modified",
            NewValue = JsonSerializer.Serialize(clientId),
            ChangedAtUtc = DateTime.UtcNow
        };

        var resolver = await ctx.BuildAuditValueResolverAsync(new List<FieldAuditLog> { log });

        resolver.Should().ContainKey(clientId.ToString());
        resolver[clientId.ToString()].Should().Be("TestClient");
    }

    [Fact]
    public async Task BuildAuditValueResolver_EmptyLogs_ReturnsEmptyDictionary()
    {
        await using var ctx = _fixture.CreateContext();

        var resolver = await ctx.BuildAuditValueResolverAsync(new List<FieldAuditLog>());

        resolver.Should().BeEmpty();
    }
}

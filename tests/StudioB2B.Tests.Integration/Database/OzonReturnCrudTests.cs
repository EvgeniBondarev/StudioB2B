using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Infrastructure.Features;
using StudioB2B.Shared;
using Xunit;

namespace StudioB2B.Tests.Integration.Database;

[Collection("Database")]
public class OzonReturnCrudTests : IClassFixture<TenantDbContextFixture>
{
    private readonly TenantDbContextFixture _fixture;

    public OzonReturnCrudTests(TenantDbContextFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task CreateOzonReturn_Persists()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var ret = DatabaseSeeder.OzonReturn();
        ctx.OrderReturns.Add(ret);
        await ctx.SaveChangesAsync();

        var loaded = await ctx.OrderReturns.AsNoTracking().FirstOrDefaultAsync(r => r.Id == ret.Id);
        loaded.Should().NotBeNull();
        loaded.Type.Should().Be("FullReturn");
    }

    [Fact]
    public async Task GetReturnsCounts_GroupsByType()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var tag = Guid.NewGuid().ToString("N");
        ctx.OrderReturns.AddRange(
            DatabaseSeeder.OzonReturn("TypeA", $"POST-{tag}-1"),
            DatabaseSeeder.OzonReturn("TypeA", $"POST-{tag}-2"),
            DatabaseSeeder.OzonReturn("TypeB", $"POST-{tag}-3"));
        await ctx.SaveChangesAsync();

        var counts = await ctx.GetReturnsCountsAsync();

        counts.TypeCounts.Should().ContainKey("TypeA");
        counts.TypeCounts["TypeA"].Should().BeGreaterThanOrEqualTo(2);
        counts.TypeCounts.Should().ContainKey("TypeB");
    }

    [Fact]
    public async Task GetReturnsPage_FiltersByPostingNumber()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var uniquePosting = $"UNIQUE-{Guid.NewGuid():N}";
        ctx.OrderReturns.Add(DatabaseSeeder.OzonReturn("Cancellation", uniquePosting));
        await ctx.SaveChangesAsync();

        var result = await ctx.GetReturnsPageAsync(new ReturnsPageRequest(
            SearchText: uniquePosting,
            Skip: 0,
            Take: 10));

        result.Items.Should().Contain(r => r.PostingNumber == uniquePosting);
        result.TotalCount.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task MultipleReturns_CancellationWithOrder_CountedSeparately()
    {
        await using var ctx = _fixture.CreateContext();
        await _fixture.SeedReferenceDataAsync();
        ctx.SuppressAudit = true;

        // GetReturnsCountsAsync counts by r.OrderId != null (FK to internal OrderEntity).
        // We need a real Shipment + Order so the OrderReturn can reference OrderId.
        var shipment = DatabaseSeeder.Shipment(_fixture.DefaultClientId);
        ctx.Shipments.Add(shipment);
        await ctx.SaveChangesAsync();

        var order = DatabaseSeeder.Order(shipment.Id);
        ctx.Orders.Add(order);
        await ctx.SaveChangesAsync();

        var cancellation = DatabaseSeeder.OzonReturn("Cancellation", $"CAN-{Guid.NewGuid():N}");
        cancellation.OrderId = order.Id;
        ctx.OrderReturns.Add(cancellation);
        await ctx.SaveChangesAsync();

        var counts = await ctx.GetReturnsCountsAsync();

        counts.CancellationsWithOrderCount.Should().BeGreaterThanOrEqualTo(1);
    }
}


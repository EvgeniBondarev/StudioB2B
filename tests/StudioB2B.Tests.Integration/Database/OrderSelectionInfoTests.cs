using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Infrastructure.Features;
using Xunit;

namespace StudioB2B.Tests.Integration.Database;

[Collection("Database")]
public class OrderSelectionInfoTests : IClassFixture<TenantDbContextFixture>
{
    private readonly TenantDbContextFixture _fixture;

    public OrderSelectionInfoTests(TenantDbContextFixture fixture)
    {
        _fixture = fixture;
        _fixture.SeedReferenceDataAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task GetOrderSelectionInfo_SingleStatus_ReturnsAvailableTransactions()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var shipment = DatabaseSeeder.Shipment(_fixture.DefaultClientId);
        ctx.Shipments.Add(shipment);
        await ctx.SaveChangesAsync();

        var order = DatabaseSeeder.Order(shipment.Id);
        order.SystemStatusId = _fixture.DefaultFromStatusId;
        ctx.Orders.Add(order);
        await ctx.SaveChangesAsync();

        var transaction = DatabaseSeeder.OrderTransaction(_fixture.DefaultFromStatusId, _fixture.DefaultToStatusId);
        ctx.OrderTransactions.Add(transaction);
        await ctx.SaveChangesAsync();

        var info = await ctx.GetOrderSelectionInfoAsync(new[] { order.Id });

        info.StatusIds.Should().Contain(_fixture.DefaultFromStatusId);
        info.AvailableTransactions.Should().Contain(t => t.Id == transaction.Id);
    }

    [Fact]
    public async Task GetOrderSelectionInfo_MixedStatuses_NoAvailableTransactions()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var shipment = DatabaseSeeder.Shipment(_fixture.DefaultClientId);
        ctx.Shipments.Add(shipment);
        await ctx.SaveChangesAsync();

        var order1 = DatabaseSeeder.Order(shipment.Id);
        order1.SystemStatusId = _fixture.DefaultFromStatusId;
        var order2 = DatabaseSeeder.Order(shipment.Id);
        order2.SystemStatusId = _fixture.DefaultToStatusId;
        ctx.Orders.AddRange(order1, order2);
        await ctx.SaveChangesAsync();

        var info = await ctx.GetOrderSelectionInfoAsync(new[] { order1.Id, order2.Id });

        info.StatusIds.Should().HaveCount(2);
        info.AvailableTransactions.Should().BeEmpty("mixed statuses => no uniform transition");
    }

    [Fact]
    public async Task GetOrderSelectionInfo_NullStatus_HasNullIsTrue()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var shipment = DatabaseSeeder.Shipment(_fixture.DefaultClientId);
        ctx.Shipments.Add(shipment);
        await ctx.SaveChangesAsync();

        var order = DatabaseSeeder.Order(shipment.Id);
        order.SystemStatusId = null;
        ctx.Orders.Add(order);
        await ctx.SaveChangesAsync();

        var info = await ctx.GetOrderSelectionInfoAsync(new[] { order.Id });

        info.HasNullStatus.Should().BeTrue();
    }
}

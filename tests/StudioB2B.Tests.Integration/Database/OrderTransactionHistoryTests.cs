using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Features;
using Xunit;

namespace StudioB2B.Tests.Integration.Database;

[Collection("Database")]
public class OrderTransactionHistoryTests : IClassFixture<TenantDbContextFixture>
{
    private readonly TenantDbContextFixture _fixture;

    public OrderTransactionHistoryTests(TenantDbContextFixture fixture)
    {
        _fixture = fixture;
        _fixture.SeedReferenceDataAsync().GetAwaiter().GetResult();
    }

    private static OrderTransactionHistory NewHistory(Guid orderId, Guid transactionId, bool success) => new()
    {
        Id = Guid.NewGuid(),
        OrderId = orderId,
        OrderTransactionId = transactionId,
        PerformedAtUtc = DateTime.UtcNow,
        Success = success
    };

    [Fact]
    public async Task GetHistoryPaged_ReturnsHistory_OrderedByDateDesc()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var shipment = DatabaseSeeder.Shipment(_fixture.DefaultClientId);
        ctx.Shipments.Add(shipment);
        await ctx.SaveChangesAsync();

        var order = DatabaseSeeder.Order(shipment.Id);
        ctx.Orders.Add(order);
        await ctx.SaveChangesAsync();

        var transaction = DatabaseSeeder.OrderTransaction(_fixture.DefaultFromStatusId, _fixture.DefaultToStatusId);
        ctx.OrderTransactions.Add(transaction);
        await ctx.SaveChangesAsync();

        var h1 = NewHistory(order.Id, transaction.Id, true);
        h1.PerformedAtUtc = DateTime.UtcNow.AddMinutes(-10);
        var h2 = NewHistory(order.Id, transaction.Id, false);
        h2.PerformedAtUtc = DateTime.UtcNow;
        ctx.OrderTransactionHistories.AddRange(h1, h2);
        await ctx.SaveChangesAsync();

        var (items, total) = await ctx.GetOrderTransactionHistoryPagedAsync(null, null, 0, 1000);

        total.Should().BeGreaterThanOrEqualTo(2);
        var ids = items.Select(i => i.Id).ToList();
        ids.Should().Contain(h1.Id);
        ids.Should().Contain(h2.Id);
        var idx1 = ids.IndexOf(h1.Id);
        var idx2 = ids.IndexOf(h2.Id);
        idx2.Should().BeLessThan(idx1, "newer record should come first (DESC order)");
    }

    [Fact]
    public async Task GetHistoryPaged_DynamicFilter_OnlySuccessful()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var shipment = DatabaseSeeder.Shipment(_fixture.DefaultClientId);
        ctx.Shipments.Add(shipment);
        await ctx.SaveChangesAsync();

        var order = DatabaseSeeder.Order(shipment.Id);
        ctx.Orders.Add(order);
        await ctx.SaveChangesAsync();

        var transaction = DatabaseSeeder.OrderTransaction(_fixture.DefaultFromStatusId, _fixture.DefaultToStatusId);
        ctx.OrderTransactions.Add(transaction);
        await ctx.SaveChangesAsync();

        var hOk = NewHistory(order.Id, transaction.Id, true);
        var hFail = NewHistory(order.Id, transaction.Id, false);
        ctx.OrderTransactionHistories.AddRange(hOk, hFail);
        await ctx.SaveChangesAsync();

        var (items, total) = await ctx.GetOrderTransactionHistoryPagedAsync("Success == true", null, 0, 1000);

        items.Should().OnlyContain(h => h.Success);
        items.Should().Contain(h => h.Id == hOk.Id);
        items.Should().NotContain(h => h.Id == hFail.Id);
    }

    [Fact]
    public async Task GetHistoryPaged_Pagination_Works()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var shipment = DatabaseSeeder.Shipment(_fixture.DefaultClientId);
        ctx.Shipments.Add(shipment);
        await ctx.SaveChangesAsync();

        var order = DatabaseSeeder.Order(shipment.Id);
        ctx.Orders.Add(order);
        await ctx.SaveChangesAsync();

        var transaction = DatabaseSeeder.OrderTransaction(_fixture.DefaultFromStatusId, _fixture.DefaultToStatusId);
        ctx.OrderTransactions.Add(transaction);
        await ctx.SaveChangesAsync();

        for (var i = 0; i < 5; i++)
            ctx.OrderTransactionHistories.Add(NewHistory(order.Id, transaction.Id, true));
        await ctx.SaveChangesAsync();

        var (page1, total) = await ctx.GetOrderTransactionHistoryPagedAsync(null, null, 0, 3);
        var (page2, _) = await ctx.GetOrderTransactionHistoryPagedAsync(null, null, 3, 3);

        total.Should().BeGreaterThanOrEqualTo(5);
        page1.Should().HaveCount(3);
        page2.Count.Should().BeGreaterThanOrEqualTo(1);
    }
}

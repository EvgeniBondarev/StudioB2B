using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Infrastructure.Services;
using StudioB2B.Infrastructure.Services.Order;
using Xunit;

namespace StudioB2B.Tests.Integration.Database;

[Collection("Database")]
public class OrderTransactionApplyTests : IClassFixture<TenantDbContextFixture>
{
    private readonly TenantDbContextFixture _fixture;

    private static readonly ICurrentUserProvider FakeUser = Mock.Of<ICurrentUserProvider>(u =>
        u.IsAuthenticated == false &&
        u.UserId == (Guid?)null &&
        u.Email == (string?)null &&
        u.Permissions == Enumerable.Empty<string>());

    public OrderTransactionApplyTests(TenantDbContextFixture fixture)
    {
        _fixture = fixture;
        _fixture.SeedReferenceDataAsync().GetAwaiter().GetResult();
    }

    private OrderTransactionService CreateService(
        StudioB2B.Infrastructure.Persistence.Tenant.TenantDbContext ctx) =>
        new(ctx, new CalculationEngine(), FakeUser, NullLogger<OrderTransactionService>.Instance);

    [Fact]
    public async Task ApplyAsync_ChangesSystemStatus_AndRecordsHistory()
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

        var svc = CreateService(ctx);
        var result = await svc.ApplyAsync(order.Id, transaction.Id);

        result.Success.Should().BeTrue(result.ErrorMessage);

        var updatedOrder = await ctx.Orders.IgnoreQueryFilters().AsNoTracking()
            .FirstAsync(o => o.Id == order.Id);
        updatedOrder.SystemStatusId.Should().Be(_fixture.DefaultToStatusId);

        var history = await ctx.OrderTransactionHistories.AsNoTracking()
            .Where(h => h.OrderId == order.Id && h.OrderTransactionId == transaction.Id)
            .ToListAsync();
        history.Should().HaveCount(1);
        history[0].Success.Should().BeTrue();
    }

    [Fact]
    public async Task ApplyAsync_WrongCurrentStatus_ReturnsFalse_AndRecordsHistory()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var shipment = DatabaseSeeder.Shipment(_fixture.DefaultClientId);
        ctx.Shipments.Add(shipment);
        await ctx.SaveChangesAsync();

        var order = DatabaseSeeder.Order(shipment.Id);
        order.SystemStatusId = _fixture.DefaultToStatusId;
        ctx.Orders.Add(order);
        await ctx.SaveChangesAsync();

        var transaction = DatabaseSeeder.OrderTransaction(_fixture.DefaultFromStatusId, _fixture.DefaultToStatusId);
        ctx.OrderTransactions.Add(transaction);
        await ctx.SaveChangesAsync();

        var svc = CreateService(ctx);
        var result = await svc.ApplyAsync(order.Id, transaction.Id);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();

        var history = await ctx.OrderTransactionHistories.AsNoTracking()
            .Where(h => h.OrderId == order.Id).ToListAsync();
        history.Should().HaveCount(1);
        history[0].Success.Should().BeFalse();
    }

    [Fact]
    public async Task ApplyAsync_DisabledTransaction_ReturnsFalse()
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
        transaction.IsEnabled = false;
        ctx.OrderTransactions.Add(transaction);
        await ctx.SaveChangesAsync();

        var svc = CreateService(ctx);
        var result = await svc.ApplyAsync(order.Id, transaction.Id);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("отключён");
    }

    [Fact]
    public async Task ApplyAsync_OrderNotFound_ReturnsFalse()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var transaction = DatabaseSeeder.OrderTransaction(_fixture.DefaultFromStatusId, _fixture.DefaultToStatusId);
        ctx.OrderTransactions.Add(transaction);
        await ctx.SaveChangesAsync();

        var svc = CreateService(ctx);
        var result = await svc.ApplyAsync(Guid.NewGuid(), transaction.Id);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("не найден");
    }
}

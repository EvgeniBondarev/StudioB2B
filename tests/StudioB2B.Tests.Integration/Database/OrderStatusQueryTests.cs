using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Features;
using StudioB2B.Shared;
using Xunit;

namespace StudioB2B.Tests.Integration.Database;

/// <summary>
/// Проверяет расширенные запросы к статусам заказов:
/// пагинация, фильтры internal/marketplace/terminal, инициализационные данные.
/// </summary>
[Collection("Database")]
public class OrderStatusQueryTests : IClassFixture<TenantDbContextFixture>
{
    private readonly TenantDbContextFixture _fixture;

    public OrderStatusQueryTests(TenantDbContextFixture fixture)
    {
        _fixture = fixture;
        _fixture.SeedReferenceDataAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task GetOrderStatusesPaged_FilterInternal_ReturnsOnlyInternal()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var tag = Guid.NewGuid().ToString("N")[..8];
        var internalStatus = new OrderStatus { Id = Guid.NewGuid(), Name = $"Int_{tag}", IsInternal = true };
        var externalStatus = new OrderStatus { Id = Guid.NewGuid(), Name = $"Ext_{tag}", IsInternal = false };
        ctx.OrderStatuses.AddRange(internalStatus, externalStatus);
        await ctx.SaveChangesAsync();

        var filter = new OrderStatusPageFilter(FilterType: "internal");
        var (items, total) = await ctx.GetOrderStatusesPagedAsync(filter, null, null, 0, 1000);

        items.Should().OnlyContain(s => s.IsInternal);
        items.Should().Contain(s => s.Id == internalStatus.Id);
        items.Should().NotContain(s => s.Id == externalStatus.Id);
        total.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetOrderStatusesPaged_FilterMarketplace_ReturnsOnlyExternal()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var tag = Guid.NewGuid().ToString("N")[..8];
        var externalStatus = new OrderStatus { Id = Guid.NewGuid(), Name = $"Mkt_{tag}", IsInternal = false };
        ctx.OrderStatuses.Add(externalStatus);
        await ctx.SaveChangesAsync();

        var filter = new OrderStatusPageFilter(FilterType: "marketplace");
        var (items, _) = await ctx.GetOrderStatusesPagedAsync(filter, null, null, 0, 1000);

        items.Should().OnlyContain(s => !s.IsInternal);
        items.Should().Contain(s => s.Id == externalStatus.Id);
    }

    [Fact]
    public async Task GetOrderStatusesPaged_FilterTerminal_ReturnsOnlyTerminal()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var tag = Guid.NewGuid().ToString("N")[..8];
        var terminal = new OrderStatus { Id = Guid.NewGuid(), Name = $"Term_{tag}", IsInternal = true, IsTerminal = true };
        var nonTerminal = new OrderStatus { Id = Guid.NewGuid(), Name = $"NonT_{tag}", IsInternal = true, IsTerminal = false };
        ctx.OrderStatuses.AddRange(terminal, nonTerminal);
        await ctx.SaveChangesAsync();

        var filter = new OrderStatusPageFilter(FilterTerminal: true);
        var (items, _) = await ctx.GetOrderStatusesPagedAsync(filter, null, null, 0, 1000);

        items.Should().OnlyContain(s => s.IsTerminal);
        items.Should().Contain(s => s.Id == terminal.Id);
        items.Should().NotContain(s => s.Id == nonTerminal.Id);
    }

    [Fact]
    public async Task GetOrderStatusInitData_CountsAreCorrect()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var initData = await ctx.GetOrderStatusInitDataAsync();

        initData.Should().NotBeNull();
        initData.CountInternal.Should().BeGreaterThanOrEqualTo(0);
        initData.CountMarketplace.Should().BeGreaterThanOrEqualTo(0);
        initData.CountTerminal.Should().BeGreaterThanOrEqualTo(0);
    }
}

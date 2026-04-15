using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Features;
using Xunit;

namespace StudioB2B.Tests.Integration.Database;

[Collection("Database")]
public class OrderStatusCrudTests : IClassFixture<TenantDbContextFixture>
{
    private readonly TenantDbContextFixture _fixture;

    public OrderStatusCrudTests(TenantDbContextFixture fixture) => _fixture = fixture;

    private static OrderStatus NewStatus(string name) =>
        new() { Id = Guid.NewGuid(), Name = name, IsTerminal = false, IsInternal = false };

    [Fact]
    public async Task CreateOrderStatus_Persists()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var status = NewStatus($"Status_{Guid.NewGuid():N}");

        await ctx.CreateOrderStatusAsync(status);

        var loaded = await ctx.OrderStatuses.AsNoTracking().FirstOrDefaultAsync(s => s.Id == status.Id);
        loaded.Should().NotBeNull();
        loaded.Name.Should().Be(status.Name);
        loaded.IsTerminal.Should().BeFalse();
        loaded.IsInternal.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateOrderStatus_ChangesName_Persists()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var status = NewStatus($"UpdStatus_{Guid.NewGuid():N}");
        await ctx.CreateOrderStatusAsync(status);

        status.Name = "UpdatedStatusName";
        await ctx.UpdateOrderStatusAsync(status);

        var loaded = await ctx.OrderStatuses.AsNoTracking().FirstAsync(s => s.Id == status.Id);
        loaded.Name.Should().Be("UpdatedStatusName");
    }

    [Fact]
    public async Task SoftDeleteOrderStatus_NotReturnedByDefaultQuery()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var status = NewStatus($"DelStatus_{Guid.NewGuid():N}");
        await ctx.CreateOrderStatusAsync(status);

        await ctx.SoftDeleteOrderStatusAsync(status);

        var found = await ctx.OrderStatuses.AsNoTracking().AnyAsync(s => s.Id == status.Id);
        found.Should().BeFalse("soft-deleted status must be excluded by global query filter");
    }

    [Fact]
    public async Task SoftDeleteOrderStatus_FoundWithIgnoreQueryFilters()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var status = NewStatus($"IgnStatus_{Guid.NewGuid():N}");
        await ctx.CreateOrderStatusAsync(status);

        await ctx.SoftDeleteOrderStatusAsync(status);

        var found = await ctx.OrderStatuses
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(s => s.Id == status.Id);
        found.Should().BeTrue("deleted status must be findable with IgnoreQueryFilters");
    }
}


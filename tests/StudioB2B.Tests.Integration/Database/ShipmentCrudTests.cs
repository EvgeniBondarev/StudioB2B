using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace StudioB2B.Tests.Integration.Database;

/// <summary>
/// Проверяет CRUD отправлений (Shipment) и связанных позиций заказа (OrderEntity).
/// </summary>
[Collection("Database")]
public class ShipmentCrudTests : IClassFixture<TenantDbContextFixture>
{
    private readonly TenantDbContextFixture _fixture;

    public ShipmentCrudTests(TenantDbContextFixture fixture)
    {
        _fixture = fixture;
        _fixture.SeedReferenceDataAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task CreateShipment_Persists()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var shipment = DatabaseSeeder.Shipment(_fixture.DefaultClientId);
        ctx.Shipments.Add(shipment);
        await ctx.SaveChangesAsync();

        var loaded = await ctx.Shipments.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == shipment.Id);

        loaded.Should().NotBeNull();
        loaded!.PostingNumber.Should().Be(shipment.PostingNumber);
        loaded.MarketplaceClientId.Should().Be(_fixture.DefaultClientId);
    }

    [Fact]
    public async Task CreateShipment_WithOrders_NavigationLoads()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var shipment = DatabaseSeeder.Shipment(_fixture.DefaultClientId);
        ctx.Shipments.Add(shipment);
        await ctx.SaveChangesAsync();

        var order1 = DatabaseSeeder.Order(shipment.Id);
        var order2 = DatabaseSeeder.Order(shipment.Id);
        ctx.Orders.AddRange(order1, order2);
        await ctx.SaveChangesAsync();

        var loaded = await ctx.Shipments
            .Include(s => s.Orders)
            .AsNoTracking()
            .FirstAsync(s => s.Id == shipment.Id);

        loaded.Orders.Should().HaveCount(2);
        loaded.Orders.Should().Contain(o => o.Id == order1.Id);
        loaded.Orders.Should().Contain(o => o.Id == order2.Id);
    }

    [Fact]
    public async Task SoftDeleteShipment_NotReturnedByDefaultQuery()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var shipment = DatabaseSeeder.Shipment(_fixture.DefaultClientId);
        ctx.Shipments.Add(shipment);
        await ctx.SaveChangesAsync();

        var entity = await ctx.Shipments.FindAsync(shipment.Id);
        entity!.IsDeleted = true;
        await ctx.SaveChangesAsync();

        var found = await ctx.Shipments.AsNoTracking().AnyAsync(s => s.Id == shipment.Id);
        found.Should().BeFalse("soft-deleted shipment must be excluded");

        var foundIgnored = await ctx.Shipments
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(s => s.Id == shipment.Id);
        foundIgnored.Should().BeTrue("must exist with IgnoreQueryFilters");
    }

    [Fact]
    public async Task OrderEntity_SoftDelete_NotReturnedByDefaultQuery()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var shipment = DatabaseSeeder.Shipment(_fixture.DefaultClientId);
        ctx.Shipments.Add(shipment);
        await ctx.SaveChangesAsync();

        var order = DatabaseSeeder.Order(shipment.Id);
        ctx.Orders.Add(order);
        await ctx.SaveChangesAsync();

        var entity = await ctx.Orders.FindAsync(order.Id);
        entity!.IsDeleted = true;
        await ctx.SaveChangesAsync();

        var found = await ctx.Orders.AsNoTracking().AnyAsync(o => o.Id == order.Id);
        found.Should().BeFalse("soft-deleted order must be excluded");

        var foundIgnored = await ctx.Orders
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(o => o.Id == order.Id);
        foundIgnored.Should().BeTrue("must exist with IgnoreQueryFilters");
    }

    [Fact]
    public async Task Shipment_HasReturn_FlagUpdates()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var shipment = DatabaseSeeder.Shipment(_fixture.DefaultClientId);
        ctx.Shipments.Add(shipment);
        await ctx.SaveChangesAsync();

        var entity = await ctx.Shipments.FindAsync(shipment.Id);
        entity!.HasReturn = true;
        await ctx.SaveChangesAsync();

        var loaded = await ctx.Shipments.AsNoTracking().FirstAsync(s => s.Id == shipment.Id);
        loaded.HasReturn.Should().BeTrue();
    }
}


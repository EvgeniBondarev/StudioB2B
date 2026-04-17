using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Infrastructure.Features;
using StudioB2B.Shared;
using Xunit;

namespace StudioB2B.Tests.Integration.Database;

[Collection("Database")]
public class OrderTransactionCrudTests : IClassFixture<TenantDbContextFixture>
{
    private readonly TenantDbContextFixture _fixture;

    public OrderTransactionCrudTests(TenantDbContextFixture fixture)
    {
        _fixture = fixture;
        _fixture.SeedReferenceDataAsync().GetAwaiter().GetResult();
    }

    private SaveOrderTransactionRequest MinimalRequest(string name) =>
        new(name, _fixture.DefaultFromStatusId, _fixture.DefaultToStatusId, true, null, null, [], []);

    [Fact]
    public async Task CreateOrderTransaction_Persists()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var name = $"Trans_{Guid.NewGuid():N}";
        var t = await ctx.CreateOrderTransactionAsync(MinimalRequest(name));

        t.Id.Should().NotBeEmpty();

        var loaded = await ctx.OrderTransactions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == t.Id);
        loaded.Should().NotBeNull();
        loaded!.Name.Should().Be(name);
        loaded.FromSystemStatusId.Should().Be(_fixture.DefaultFromStatusId);
        loaded.ToSystemStatusId.Should().Be(_fixture.DefaultToStatusId);
    }

    [Fact]
    public async Task UpdateOrderTransaction_ChangesName_Persists()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var t = await ctx.CreateOrderTransactionAsync(MinimalRequest($"T_{Guid.NewGuid():N}"));

        var newName = "UpdatedTransaction";
        var ok = await ctx.UpdateOrderTransactionAsync(t.Id, MinimalRequest(newName));

        ok.Should().BeTrue();

        var loaded = await ctx.OrderTransactions.AsNoTracking().FirstAsync(x => x.Id == t.Id);
        loaded.Name.Should().Be(newName);
    }

    [Fact]
    public async Task DeleteOrderTransaction_SoftDeletes()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var t = await ctx.CreateOrderTransactionAsync(MinimalRequest($"Del_{Guid.NewGuid():N}"));

        var ok = await ctx.SoftDeleteOrderTransactionAsync(t.Id);
        ok.Should().BeTrue();

        var found = await ctx.OrderTransactions.AsNoTracking().AnyAsync(x => x.Id == t.Id);
        found.Should().BeFalse("soft-deleted transaction must be excluded by query filter");

        var foundIgnored = await ctx.OrderTransactions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(x => x.Id == t.Id);
        foundIgnored.Should().BeTrue("soft-deleted transaction must exist with IgnoreQueryFilters");
    }

    [Fact]
    public async Task GetOrderTransactionsPaged_ExcludesDeleted()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var alive = await ctx.CreateOrderTransactionAsync(MinimalRequest($"Alive_{Guid.NewGuid():N}"));
        var dead = await ctx.CreateOrderTransactionAsync(MinimalRequest($"Dead_{Guid.NewGuid():N}"));
        await ctx.SoftDeleteOrderTransactionAsync(dead.Id);

        var (items, _) = await ctx.GetOrderTransactionsPagedAsync(null, null, 0, 1000);

        items.Should().Contain(x => x.Id == alive.Id);
        items.Should().NotContain(x => x.Id == dead.Id);
    }
}


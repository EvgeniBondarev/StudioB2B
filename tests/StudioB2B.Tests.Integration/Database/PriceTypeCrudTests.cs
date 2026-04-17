using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Infrastructure.Features;
using Xunit;

namespace StudioB2B.Tests.Integration.Database;

[Collection("Database")]
public class PriceTypeCrudTests : IClassFixture<TenantDbContextFixture>
{
    private readonly TenantDbContextFixture _fixture;

    public PriceTypeCrudTests(TenantDbContextFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task CreatePriceType_Persists()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var pt = DatabaseSeeder.PriceType();
        await ctx.CreatePriceTypeAsync(pt);

        var loaded = await ctx.PriceTypes.AsNoTracking().FirstOrDefaultAsync(p => p.Id == pt.Id);
        loaded.Should().NotBeNull();
        loaded!.Name.Should().Be(pt.Name);
        loaded.IsUserDefined.Should().BeTrue();
    }

    [Fact]
    public async Task UpdatePriceType_ChangesName_Persists()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var pt = DatabaseSeeder.PriceType();
        await ctx.CreatePriceTypeAsync(pt);

        pt.Name = "UpdatedPriceType";
        var ok = await ctx.UpdatePriceTypeAsync(pt);

        ok.Should().BeTrue();

        var loaded = await ctx.PriceTypes.AsNoTracking().FirstAsync(p => p.Id == pt.Id);
        loaded.Name.Should().Be("UpdatedPriceType");
    }

    [Fact]
    public async Task UpdatePriceType_SystemType_ReturnsFalse()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var pt = DatabaseSeeder.PriceType(isUserDefined: false);
        ctx.PriceTypes.Add(pt);
        await ctx.SaveChangesAsync();

        pt.Name = "CannotUpdate";
        var ok = await ctx.UpdatePriceTypeAsync(pt);

        ok.Should().BeFalse("system price types must not be updated");
    }

    [Fact]
    public async Task SoftDeletePriceType_NotReturnedByDefaultQuery()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var pt = DatabaseSeeder.PriceType();
        await ctx.CreatePriceTypeAsync(pt);

        var ok = await ctx.SoftDeletePriceTypeAsync(pt.Id);
        ok.Should().BeTrue();

        var found = await ctx.PriceTypes.AsNoTracking().AnyAsync(p => p.Id == pt.Id);
        found.Should().BeFalse("soft-deleted price type must be excluded by global query filter");
    }

    [Fact]
    public async Task GetPriceTypesPaged_FiltersByName()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var unique = $"UniqueTag_{Guid.NewGuid():N}";
        var pt = DatabaseSeeder.PriceType(name: unique);
        await ctx.CreatePriceTypeAsync(pt);

        var (items, count) = await ctx.GetPriceTypesPagedAsync(
            $"Name.Contains(\"{unique}\")", null, 0, 50);

        count.Should().BeGreaterThanOrEqualTo(1);
        items.Should().Contain(p => p.Id == pt.Id);
    }
}


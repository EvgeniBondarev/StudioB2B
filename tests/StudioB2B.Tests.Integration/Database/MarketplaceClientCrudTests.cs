using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudioB2B.Infrastructure.Features;
using StudioB2B.Infrastructure.Profiles;
using StudioB2B.Shared;
using Xunit;

namespace StudioB2B.Tests.Integration.Database;

[Collection("Database")]
public class MarketplaceClientCrudTests : IClassFixture<TenantDbContextFixture>
{
    private readonly TenantDbContextFixture _fixture;

    private static readonly IMapper Mapper = new ServiceCollection()
        .AddLogging()
        .AddAutoMapper(cfg => cfg.AddProfile(new MarketplaceMappingProfile()))
        .BuildServiceProvider()
        .GetRequiredService<IMapper>();

    public MarketplaceClientCrudTests(TenantDbContextFixture fixture)
    {
        _fixture = fixture;
        _fixture.SeedReferenceDataAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsSeededClient()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var all = await ctx.MarketplaceClients!.GetAllAsync(Mapper);

        all.Should().Contain(c => c.Id == _fixture.DefaultClientId);
    }

    [Fact]
    public async Task CreateAsync_Persists()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var dto = new CreateMarketplaceClientDto(
            $"NewClient_{Guid.NewGuid():N}",
            Guid.NewGuid().ToString("N"),
            Guid.NewGuid().ToString("N"),
            _fixture.DefaultClientTypeId,
            []);

        var result = await ctx.CreateAsync(dto, Mapper);

        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();

        var loaded = await ctx.MarketplaceClients!
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == result.Id);
        loaded.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_ChangesName_Persists()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var client = DatabaseSeeder.MarketplaceClient(_fixture.DefaultClientTypeId);
        ctx.MarketplaceClients!.Add(client);
        await ctx.SaveChangesAsync();

        var updateDto = new UpdateMarketplaceClientDto(
            client.Id,
            "UpdatedClientName",
            client.ApiId,
            client.Key,
            _fixture.DefaultClientTypeId,
            []);

        var updated = await ctx.UpdateAsync(updateDto, Mapper);

        updated.Should().NotBeNull();
        updated!.Name.Should().Be("UpdatedClientName");
    }

    [Fact]
    public async Task DeleteAsync_RemovesClient()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var client = DatabaseSeeder.MarketplaceClient(_fixture.DefaultClientTypeId);
        ctx.MarketplaceClients!.Add(client);
        await ctx.SaveChangesAsync();

        var deleted = await ctx.DeleteAsync(client.Id);


        var found = await ctx.MarketplaceClients!
            .AsNoTracking()
            .AnyAsync(c => c.Id == client.Id);
        found.Should().BeFalse();
    }

    [Fact]
    public async Task GetClientOptionsAsync_WithAllowedIds_FiltersCorrectly()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var allowed = DatabaseSeeder.MarketplaceClient(_fixture.DefaultClientTypeId, "Allowed-Client");
        var other = DatabaseSeeder.MarketplaceClient(_fixture.DefaultClientTypeId, "Other-Client");
        ctx.MarketplaceClients!.AddRange(allowed, other);
        await ctx.SaveChangesAsync();

        var options = await ctx.GetClientOptionsAsync([allowed.Id]);

        options.Should().Contain(o => o.Id == allowed.Id);
        options.Should().NotContain(o => o.Id == other.Id);
    }
}


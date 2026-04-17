using FluentAssertions;
using StudioB2B.Infrastructure.Features;
using Xunit;

namespace StudioB2B.Tests.Integration.Database;

/// <summary>
/// Проверяет GetInitDataAsync: счётчики клиентов по типу и по режиму.
/// </summary>
[Collection("Database")]
public class MarketplaceClientInitDataTests : IClassFixture<TenantDbContextFixture>
{
    private readonly TenantDbContextFixture _fixture;

    public MarketplaceClientInitDataTests(TenantDbContextFixture fixture)
    {
        _fixture = fixture;
        _fixture.SeedReferenceDataAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task GetInitData_ContainsSeededClientType()
    {
        await using var ctx = _fixture.CreateContext();

        var initData = await ctx.GetInitDataAsync();

        initData.Types.Should().NotBeNull();
        initData.Types.Should().Contain(t => t.Id == _fixture.DefaultClientTypeId);
    }

    [Fact]
    public async Task GetInitData_CountsByType_IncludesSeededClient()
    {
        await using var ctx = _fixture.CreateContext();

        var initData = await ctx.GetInitDataAsync();

        initData.CountsByTypeId.Should().NotBeNull();
        initData.CountsByTypeId.Should().ContainKey(_fixture.DefaultClientTypeId);
        initData.CountsByTypeId[_fixture.DefaultClientTypeId].Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetInitData_CountsByMode_IncludesSeededMode()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        // Создаём клиента с DefaultMode чтобы счётчик точно был
        var client = DatabaseSeeder.MarketplaceClient(_fixture.DefaultClientTypeId);
        client.ModeId = _fixture.DefaultModeId;
        ctx.MarketplaceClients!.Add(client);
        await ctx.SaveChangesAsync();

        var initData = await ctx.GetInitDataAsync();

        initData.CountsByModeId.Should().ContainKey(_fixture.DefaultModeId);
        initData.CountsByModeId[_fixture.DefaultModeId].Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetInitData_Modes_ContainsSeededMode()
    {
        await using var ctx = _fixture.CreateContext();

        var initData = await ctx.GetInitDataAsync();

        initData.Modes.Should().Contain(m => m.Id == _fixture.DefaultModeId);
    }
}

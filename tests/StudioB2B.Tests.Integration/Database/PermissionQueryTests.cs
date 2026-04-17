using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Infrastructure.Features;
using StudioB2B.Infrastructure.Persistence.Tenant;
using Xunit;

namespace StudioB2B.Tests.Integration.Database;

[Collection("Database")]
public class PermissionQueryTests : IClassFixture<TenantDbContextFixture>
{
    private readonly TenantDbContextFixture _fixture;

    public PermissionQueryTests(TenantDbContextFixture fixture)
    {
        _fixture = fixture;
        _fixture.SeedReferenceDataAsync().GetAwaiter().GetResult();
    }

    private static async Task RunSeedAsync(TenantDbContext ctx)
    {
        var method = typeof(StudioB2B.Infrastructure.Services.MultiTenancy.TenantDatabaseInitializer)
            .GetMethod("SeedPagesColumnsAndFunctionsAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        method.Should().NotBeNull();
        var task = (System.Threading.Tasks.Task)method!.Invoke(null, [ctx, System.Threading.CancellationToken.None])!;
        await task;
    }

    [Fact]
    public async Task GetPagesWithDetailsAsync_ReturnsPagesWithColumnsAndFunctions()
    {
        await using var ctx = _fixture.CreateContext();
        await RunSeedAsync(ctx);

        var pages = await ctx.GetPagesWithDetailsAsync();

        pages.Should().NotBeEmpty();
        pages.Should().OnlyContain(p => p.Name != null);
    }

    [Fact]
    public async Task GetEntityOptionsForPermission_ReturnsAllEntityTypes()
    {
        await using var ctx = _fixture.CreateContext();
        await RunSeedAsync(ctx);

        var options = await ctx.GetEntityOptionsForPermissionAsync();

        options.Should().ContainKey("Warehouse");
        options.Should().ContainKey("OrderStatus");
        options.Should().ContainKey("MarketplaceClient");
    }

    [Fact]
    public async Task GetAvailablePermissionsAsync_ReturnsCreatedPermission()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var dto = new StudioB2B.Shared.CreatePermissionDto(
            $"Perm_{Guid.NewGuid():N}",
            IsFullAccess: false,
            Pages: [],
            PageColumns: [],
            Functions: [],
            BlockedEntities: []);
        var (ok, _, id) = await ctx.CreatePermissionAsync(dto);
        ok.Should().BeTrue();

        var available = await ctx.GetAvailablePermissionsAsync();

        available.Should().Contain(p => p.Value == id.ToString());
    }

    [Fact]
    public async Task GetPermissionByIdAsync_ReturnsCorrectPermission()
    {
        await using var ctx = _fixture.CreateContext();
        ctx.SuppressAudit = true;

        var name = $"ByIdPerm_{Guid.NewGuid():N}";
        var dto = new StudioB2B.Shared.CreatePermissionDto(
            name,
            IsFullAccess: false,
            Pages: [],
            PageColumns: [],
            Functions: [],
            BlockedEntities: []);
        var (_, _, id) = await ctx.CreatePermissionAsync(dto);

        var result = await ctx.GetPermissionByIdAsync(id);

        result.Should().NotBeNull();
        result!.Name.Should().Be(name);
    }
}

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Domain.Constants;
using StudioB2B.Infrastructure.Persistence.Tenant;
using StudioB2B.Tests.Integration.Database;
using Xunit;

namespace StudioB2B.Tests.Integration.Seed;

/// <summary>
/// Integration tests for TenantDatabaseInitializer.SeedPagesColumnsAndFunctionsAsync.
/// Verifies that every enum value is seeded into the database and that seeding is idempotent.
/// </summary>
[Collection("Database")]
public class SeedTests : IClassFixture<TenantDbContextFixture>
{
    private readonly TenantDbContextFixture _fixture;

    public SeedTests(TenantDbContextFixture fixture) => _fixture = fixture;

    private static async Task RunSeedAsync(TenantDbContext ctx)
    {
        // Invoke the private SeedPagesColumnsAndFunctionsAsync via reflection
        var method = typeof(StudioB2B.Infrastructure.Services.MultiTenancy.TenantDatabaseInitializer)
            .GetMethod("SeedPagesColumnsAndFunctionsAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        method.Should().NotBeNull("SeedPagesColumnsAndFunctionsAsync must exist");

        var task = (Task)method!.Invoke(null, [ctx, CancellationToken.None])!;
        await task;
    }

    [Fact]
    public async Task Seed_CreatesAllPageEnumRows()
    {
        await using var ctx = _fixture.CreateContext();
        await RunSeedAsync(ctx);

        var pageNames = await ctx.Pages.AsNoTracking()
            .Select(p => p.Name).ToListAsync();

        foreach (var page in Enum.GetValues<PageEnum>())
            pageNames.Should().Contain(page.ToString(),
                $"Page row for PageEnum.{page} must be seeded");
    }

    [Fact]
    public async Task Seed_CreatesAllFunctionEnumRows()
    {
        await using var ctx = _fixture.CreateContext();
        await RunSeedAsync(ctx);

        var funcNames = await ctx.Functions.AsNoTracking()
            .Select(f => f.Name).ToListAsync();

        // Only functions present in FunctionPageMap are seeded
        var map = GetFunctionPageMap();
        foreach (var func in Enum.GetValues<FunctionEnum>())
        {
            if (!map.ContainsKey(func)) continue;
            funcNames.Should().Contain(func.ToString(),
                $"Function row for FunctionEnum.{func} must be seeded");
        }
    }

    [Fact]
    public async Task Seed_IsIdempotent_NoDuplicates()
    {
        await using var ctx = _fixture.CreateContext();
        await RunSeedAsync(ctx);
        await RunSeedAsync(ctx); // second call

        var pageCount = await ctx.Pages.AsNoTracking().CountAsync();
        pageCount.Should().Be(Enum.GetValues<PageEnum>().Length,
            "running seed twice must not create duplicate Page rows");
    }

    [Fact]
    public async Task Seed_PageRows_HaveNonEmptyDisplayName()
    {
        await using var ctx = _fixture.CreateContext();
        await RunSeedAsync(ctx);

        var empty = await ctx.Pages.AsNoTracking()
            .Where(p => p.DisplayName == null || p.DisplayName == "")
            .Select(p => p.Name)
            .ToListAsync();

        empty.Should().BeEmpty("all seeded Page rows must have a DisplayName from [Description]");
    }

    private static Dictionary<FunctionEnum, PageEnum> GetFunctionPageMap()
    {
        var field = typeof(StudioB2B.Infrastructure.Services.MultiTenancy.TenantDatabaseInitializer)
            .GetField("FunctionPageMap",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return (Dictionary<FunctionEnum, PageEnum>)field!.GetValue(null)!;
    }
}

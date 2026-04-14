using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Infrastructure.Features;
using StudioB2B.Infrastructure.Services.MultiTenancy;
using StudioB2B.Shared;
using Xunit;

namespace StudioB2B.Tests.Integration.Database;

[Collection("Database")]
public class PermissionCrudTests : IClassFixture<TenantDbContextFixture>
{
    private readonly TenantDbContextFixture _fixture;

    public PermissionCrudTests(TenantDbContextFixture fixture) => _fixture = fixture;

    private static async Task SeedAsync(StudioB2B.Infrastructure.Persistence.Tenant.TenantDbContext ctx)
    {
        var method = typeof(TenantDatabaseInitializer)
            .GetMethod("SeedPagesColumnsAndFunctionsAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var task = (Task)method!.Invoke(null, [ctx, CancellationToken.None])!;
        await task;
    }

    [Fact]
    public async Task CreatePermission_PersistsToDatabase()
    {
        await using var ctx = _fixture.CreateContext();
        await SeedAsync(ctx);

        var dto = new CreatePermissionDto(
            $"TestPerm_{Guid.NewGuid():N}",
            false,
            [],
            [],
            [],
            []);

        var (success, error, id) = await ctx.CreatePermissionAsync(dto);

        success.Should().BeTrue(error);
        id.Should().NotBe(Guid.Empty);

        var inDb = await ctx.Permissions.FindAsync(id);
        inDb.Should().NotBeNull();
        inDb!.Name.Should().Be(dto.Name);
    }

    [Fact]
    public async Task DeletePermission_SoftDeletes()
    {
        await using var ctx = _fixture.CreateContext();
        await SeedAsync(ctx);

        var dto = new CreatePermissionDto(
            $"ToDelete_{Guid.NewGuid():N}",
            false,
            [], [], [], []);
        var (_, _, id) = await ctx.CreatePermissionAsync(dto);

        var (success, _) = await ctx.DeletePermissionAsync(id);
        success.Should().BeTrue();

        // Should not be returned by normal query (soft-delete filter)
        var visible = await ctx.Permissions.AsNoTracking().AnyAsync(p => p.Id == id);
        visible.Should().BeFalse("soft-deleted permissions must be filtered out");
    }

    [Fact]
    public async Task CreatePermission_DuplicateName_Fails()
    {
        await using var ctx = _fixture.CreateContext();
        await SeedAsync(ctx);

        var name = $"Dup_{Guid.NewGuid():N}";
        var dto = new CreatePermissionDto(name, false, [], [], [], []);

        var (ok1, _, _) = await ctx.CreatePermissionAsync(dto);
        var (ok2, err2, _) = await ctx.CreatePermissionAsync(dto);

        ok1.Should().BeTrue();
        ok2.Should().BeFalse();
        err2.Should().NotBeNullOrEmpty();
    }
}

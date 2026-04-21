using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Persistence.Tenant;
using StudioB2B.Infrastructure.Services.MultiTenancy;
using Xunit;

namespace StudioB2B.Tests.Integration.Database;

[Collection("Database")]
public class SoftDeleteFilterTests : IClassFixture<TenantDbContextFixture>
{
    private readonly TenantDbContextFixture _fixture;

    public SoftDeleteFilterTests(TenantDbContextFixture fixture) => _fixture = fixture;

    private static async Task SeedAsync(TenantDbContext ctx)
    {
        var method = typeof(TenantDatabaseInitializer)
            .GetMethod("SeedPagesColumnsAndFunctionsAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var task = (Task)method!.Invoke(null, [ctx, CancellationToken.None])!;
        await task;
    }

    [Fact]
    public async Task SoftDeleted_Permission_NotReturnedByDefaultQuery()
    {
        await using var ctx = _fixture.CreateContext();
        await SeedAsync(ctx);

        ctx.SuppressAudit = true;

        // Create a permission
        var perm = new Permission
        {
            Id = Guid.NewGuid(),
            Name = $"SoftDeleteTest_{Guid.NewGuid():N}",
            IsFullAccess = false
        };
        ctx.Permissions.Add(perm);
        await ctx.SaveChangesAsync();

        // Soft-delete it
        perm.IsDeleted = true;
        await ctx.SaveChangesAsync();

        // Default query should not return it
        var found = await ctx.Permissions.AsNoTracking().AnyAsync(p => p.Id == perm.Id);
        found.Should().BeFalse("soft-deleted entities must be excluded by the global query filter");
    }

    [Fact]
    public async Task SoftDeleted_CanBeFoundWithIgnoreFilter()
    {
        await using var ctx = _fixture.CreateContext();
        await SeedAsync(ctx);

        ctx.SuppressAudit = true;

        var perm = new Permission
        {
            Id = Guid.NewGuid(),
            Name = $"IgnoreFilterTest_{Guid.NewGuid():N}",
            IsFullAccess = false
        };
        ctx.Permissions.Add(perm);
        await ctx.SaveChangesAsync();

        perm.IsDeleted = true;
        await ctx.SaveChangesAsync();

        var found = await ctx.Permissions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(p => p.Id == perm.Id);

        found.Should().BeTrue("IgnoreQueryFilters must bypass the soft-delete filter");
    }
}

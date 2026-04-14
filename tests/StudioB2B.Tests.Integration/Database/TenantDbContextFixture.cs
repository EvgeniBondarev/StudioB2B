using Microsoft.EntityFrameworkCore;
using StudioB2B.Infrastructure.Persistence.Tenant;
using Testcontainers.MySql;
using Xunit;

namespace StudioB2B.Tests.Integration.Database;

/// <summary>
/// xUnit class fixture that spins up a MySQL Testcontainer, applies all tenant migrations,
/// and seeds the base permission tables via SeedPagesColumnsAndFunctionsAsync.
/// Share one container per test class by implementing IClassFixture&lt;TenantDbContextFixture&gt;.
/// </summary>
public sealed class TenantDbContextFixture : IAsyncLifetime
{
    private readonly MySqlContainer _container = new MySqlBuilder("mysql:8.0")
        .WithDatabase("studiob2b_test")
        .WithUsername("testuser")
        .WithPassword("testpass")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Apply all tenant EF migrations
        await using var ctx = CreateContext();
        await ctx.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public TenantDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseMySql(ConnectionString, ServerVersion.AutoDetect(ConnectionString))
            .Options;
        return new TenantDbContext(options, currentUserProvider: null);
    }
}

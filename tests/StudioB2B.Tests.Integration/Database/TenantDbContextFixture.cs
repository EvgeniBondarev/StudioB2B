using Microsoft.EntityFrameworkCore;
using StudioB2B.Domain.Entities;
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

    // Reference data Guids populated by SeedReferenceDataAsync
    public Guid DefaultClientTypeId { get; private set; }
    public Guid DefaultModeId { get; private set; }
    public Guid DefaultClientId { get; private set; }
    public Guid DefaultFromStatusId { get; private set; }
    public Guid DefaultToStatusId { get; private set; }
    public Guid DefaultUserId { get; private set; }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

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

    /// <summary>
    /// Seeds minimal reference data needed by integration tests:
    /// one MarketplaceClientType, one MarketplaceClientMode, one MarketplaceClient,
    /// and two OrderStatuses (from / to).
    /// Idempotent — safe to call multiple times.
    /// </summary>
    public async Task SeedReferenceDataAsync()
    {
        await using var ctx = CreateContext();
        ctx.SuppressAudit = true;

        // MarketplaceClientType
        var clientType = await ctx.MarketplaceClientTypes!
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Name == "Ozon-Test");
        if (clientType == null)
        {
            clientType = new MarketplaceClientType { Id = Guid.NewGuid(), Name = "Ozon-Test" };
            ctx.MarketplaceClientTypes!.Add(clientType);
            await ctx.SaveChangesAsync();
        }
        DefaultClientTypeId = clientType.Id;

        // MarketplaceClientMode
        var mode = await ctx.MarketplaceClientModes!
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.Name == "FBS-Test");
        if (mode == null)
        {
            mode = new MarketplaceClientMode { Id = Guid.NewGuid(), Name = "FBS-Test" };
            ctx.MarketplaceClientModes!.Add(mode);
            await ctx.SaveChangesAsync();
        }
        DefaultModeId = mode.Id;

        // MarketplaceClient
        var client = await ctx.MarketplaceClients!
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Name == "TestClient");
        if (client == null)
        {
            client = new MarketplaceClient
            {
                Id = Guid.NewGuid(),
                Name = "TestClient",
                ApiId = "test-api-id",
                Key = "test-key",
                ClientTypeId = clientType.Id
            };
            ctx.MarketplaceClients!.Add(client);
            await ctx.SaveChangesAsync();
        }
        DefaultClientId = client.Id;

        // OrderStatus — From
        var fromStatus = await ctx.OrderStatuses
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Name == "Test-From");
        if (fromStatus == null)
        {
            fromStatus = new OrderStatus
            {
                Id = Guid.NewGuid(),
                Name = "Test-From",
                IsInternal = true
            };
            ctx.OrderStatuses.Add(fromStatus);
            await ctx.SaveChangesAsync();
        }
        DefaultFromStatusId = fromStatus.Id;

        // OrderStatus — To
        var toStatus = await ctx.OrderStatuses
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Name == "Test-To");
        if (toStatus == null)
        {
            toStatus = new OrderStatus
            {
                Id = Guid.NewGuid(),
                Name = "Test-To",
                IsInternal = true,
                IsTerminal = true
            };
            ctx.OrderStatuses.Add(toStatus);
            await ctx.SaveChangesAsync();
        }
        DefaultToStatusId = toStatus.Id;

        // TenantUser
        var user = await ctx.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == "test@test.local");
        if (user == null)
        {
            user = new TenantUser
            {
                Id = Guid.NewGuid(),
                Email = "test@test.local",
                FirstName = "Test",
                LastName = "User",
                HashPassword = "",
                IsActive = true
            };
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();
        }
        DefaultUserId = user.Id;
    }
}

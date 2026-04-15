using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StudioB2B.Domain.Constants;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Infrastructure.Services.MultiTenancy;
using StudioB2B.Tests.Integration.Database;
using Xunit;

namespace StudioB2B.Tests.Integration.Seed;

/// <summary>
/// Integration tests for TenantDatabaseInitializer.MigrateAndSeedAsync.
/// Verifies that all base seed data is created and that seeding is idempotent.
/// </summary>
[Collection("Database")]
public class TenantDatabaseInitializerTests : IClassFixture<TenantDbContextFixture>
{
    private readonly TenantDbContextFixture _fixture;
    private readonly TenantDatabaseInitializer _initializer;

    public TenantDatabaseInitializerTests(TenantDbContextFixture fixture)
    {
        _fixture = fixture;

        var ozonApi = new Mock<IOzonApiClient>();
        var keyEncryption = new Mock<IKeyEncryptionService>();
        keyEncryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns((string s) => s);

        var configuration = new ConfigurationBuilder().Build();

        _initializer = new TenantDatabaseInitializer(
            NullLogger<TenantDatabaseInitializer>.Instance,
            configuration,
            ozonApi.Object,
            keyEncryption.Object);
    }

    [Fact]
    public async Task MigrateAndSeedAsync_CreatesRobotUser()
    {
        await _initializer.MigrateAndSeedAsync(_fixture.ConnectionString, "test", CancellationToken.None);

        await using var ctx = _fixture.CreateContext();
        var exists = await ctx.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Id == SystemUser.RobotId);

        exists.Should().BeTrue("robot user must be seeded by MigrateAndSeedAsync");
    }

    [Fact]
    public async Task MigrateAndSeedAsync_RobotUser_HasCorrectEmail()
    {
        await _initializer.MigrateAndSeedAsync(_fixture.ConnectionString, "test", CancellationToken.None);

        await using var ctx = _fixture.CreateContext();
        var robot = await ctx.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == SystemUser.RobotId);

        robot.Should().NotBeNull();
        robot!.Email.Should().Be(SystemUser.RobotEmail);
        robot.IsActive.Should().BeFalse("robot user must be inactive");
    }

    [Fact]
    public async Task MigrateAndSeedAsync_SeedsMarketplaceClientTypes()
    {
        await _initializer.MigrateAndSeedAsync(_fixture.ConnectionString, "test", CancellationToken.None);

        await using var ctx = _fixture.CreateContext();
        var names = await ctx.Set<MarketplaceClientType>()
            .AsNoTracking()
            .Select(t => t.Name)
            .ToListAsync();

        names.Should().Contain("Ozon");
        names.Should().Contain("Wildberries");
        names.Should().Contain("Яндекс.Маркет");
    }

    [Fact]
    public async Task MigrateAndSeedAsync_SeedsBasePriceTypes()
    {
        await _initializer.MigrateAndSeedAsync(_fixture.ConnectionString, "test", CancellationToken.None);

        await using var ctx = _fixture.CreateContext();
        var names = await ctx.Set<PriceType>()
            .AsNoTracking()
            .Select(pt => pt.Name)
            .ToListAsync();

        names.Should().Contain("Цена");
        names.Should().Contain("Цена до скидки");
        names.Should().Contain("Себестоимость");
        names.Should().Contain("Скидка");
        names.Should().Contain("Маржа");
    }

    [Fact]
    public async Task MigrateAndSeedAsync_SeedsBaseCalculationRules()
    {
        await _initializer.MigrateAndSeedAsync(_fixture.ConnectionString, "test", CancellationToken.None);

        await using var ctx = _fixture.CreateContext();
        var resultKeys = await ctx.Set<CalculationRule>()
            .AsNoTracking()
            .Select(r => r.ResultKey)
            .ToListAsync();

        resultKeys.Should().Contain("Скидка");
        resultKeys.Should().Contain("Маржа");
    }

    [Fact]
    public async Task MigrateAndSeedAsync_SeedsBaseOrderTransactions()
    {
        await _initializer.MigrateAndSeedAsync(_fixture.ConnectionString, "test", CancellationToken.None);

        await using var ctx = _fixture.CreateContext();
        var txNames = await ctx.Set<OrderTransaction>()
            .AsNoTracking()
            .Select(t => t.Name)
            .ToListAsync();

        txNames.Should().Contain("В работу");
        txNames.Should().Contain("Готов к отгрузке");
        txNames.Should().Contain("Отгружен");
    }

    [Fact]
    public async Task MigrateAndSeedAsync_SeedsSystemOrderStatuses()
    {
        await _initializer.MigrateAndSeedAsync(_fixture.ConnectionString, "test", CancellationToken.None);

        await using var ctx = _fixture.CreateContext();
        var internalStatuses = await ctx.Set<OrderStatus>()
            .AsNoTracking()
            .Where(s => s.IsInternal)
            .Select(s => s.Name)
            .ToListAsync();

        internalStatuses.Should().Contain("Не указан");
        internalStatuses.Should().Contain("Готов к отгрузке");
        internalStatuses.Should().Contain("Отменен");
        internalStatuses.Should().Contain("Доставлен");
    }

    [Fact]
    public async Task MigrateAndSeedAsync_SeedsOzonShipmentStatuses()
    {
        await _initializer.MigrateAndSeedAsync(_fixture.ConnectionString, "test", CancellationToken.None);

        await using var ctx = _fixture.CreateContext();
        var synonyms = await ctx.Set<OrderStatus>()
            .AsNoTracking()
            .Where(s => s.Synonym != null)
            .Select(s => s.Synonym!)
            .ToListAsync();

        synonyms.Should().Contain("awaiting_deliver");
        synonyms.Should().Contain("cancelled");
        synonyms.Should().Contain("delivered");
    }

    [Fact]
    public async Task MigrateAndSeedAsync_IsIdempotent_NoExtraRobotUsers()
    {
        await _initializer.MigrateAndSeedAsync(_fixture.ConnectionString, "test", CancellationToken.None);
        await _initializer.MigrateAndSeedAsync(_fixture.ConnectionString, "test", CancellationToken.None);

        await using var ctx = _fixture.CreateContext();
        var robotCount = await ctx.Users
            .IgnoreQueryFilters()
            .CountAsync(u => u.Id == SystemUser.RobotId);

        robotCount.Should().Be(1, "robot user must not be duplicated on second seed");
    }

    [Fact]
    public async Task MigrateAndSeedAsync_IsIdempotent_NoDuplicateMarketplaceClientTypes()
    {
        await _initializer.MigrateAndSeedAsync(_fixture.ConnectionString, "test", CancellationToken.None);
        await _initializer.MigrateAndSeedAsync(_fixture.ConnectionString, "test", CancellationToken.None);

        await using var ctx = _fixture.CreateContext();
        var ozonCount = await ctx.Set<MarketplaceClientType>()
            .AsNoTracking()
            .CountAsync(t => t.Name == "Ozon");

        ozonCount.Should().Be(1, "Ozon client type must not be duplicated on second seed");
    }

    [Fact]
    public async Task MigrateAndSeedAsync_IsIdempotent_NoDuplicatePriceTypes()
    {
        await _initializer.MigrateAndSeedAsync(_fixture.ConnectionString, "test", CancellationToken.None);
        await _initializer.MigrateAndSeedAsync(_fixture.ConnectionString, "test", CancellationToken.None);

        await using var ctx = _fixture.CreateContext();
        var priceCount = await ctx.Set<PriceType>()
            .AsNoTracking()
            .CountAsync(pt => pt.Name == "Цена");

        priceCount.Should().Be(1, "price type 'Цена' must not be duplicated on second seed");
    }

    [Fact]
    public async Task MigrateAndSeedAsync_IsIdempotent_NoDuplicateCalculationRules()
    {
        await _initializer.MigrateAndSeedAsync(_fixture.ConnectionString, "test", CancellationToken.None);
        await _initializer.MigrateAndSeedAsync(_fixture.ConnectionString, "test", CancellationToken.None);

        await using var ctx = _fixture.CreateContext();
        var discountCount = await ctx.Set<CalculationRule>()
            .AsNoTracking()
            .CountAsync(r => r.ResultKey == "Скидка");

        discountCount.Should().Be(1, "calculation rule 'Скидка' must not be duplicated on second seed");
    }
}


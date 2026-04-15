using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Infrastructure.Persistence.Tenant;
using StudioB2B.Infrastructure.Services.Order;
using StudioB2B.Shared;
using Xunit;

namespace StudioB2B.Tests.Unit.Services;

public class OrderSyncServiceTests
{
    private static TenantDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new TenantDbContext(options, currentUserProvider: null);
    }

    private static void SeedOzonClient(TenantDbContext ctx, string modeName = "FBS")
    {
        var ozonType = new MarketplaceClientType { Id = Guid.NewGuid(), Name = "Ozon" };
        var mode = new MarketplaceClientMode { Id = Guid.NewGuid(), Name = modeName };
        var client = new MarketplaceClient
        {
            Id = Guid.NewGuid(),
            Name = "Test Client",
            ApiId = "12345",
            ClientTypeId = ozonType.Id,
            ClientType = ozonType,
            ModeId = mode.Id,
            Mode = mode
        };

        ctx.MarketplaceClientTypes!.Add(ozonType);
        ctx.MarketplaceClientModes!.Add(mode);
        ctx.MarketplaceClients!.Add(client);
        ctx.SuppressAudit = true;
        ctx.SaveChanges();
    }

    private static Mock<IOrderAdapter> MockAdapter(string modeName, OrderSyncResultDto result)
    {
        var mock = new Mock<IOrderAdapter>();
        mock.Setup(a => a.ClientModeName).Returns(modeName);
        mock.Setup(a => a.SyncAsync(It.IsAny<MarketplaceClient>(), It.IsAny<DateTime>(),
                It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
        return mock;
    }

    [Fact]
    public async Task SyncAllAsync_SingleClient_AggregatesAdapterResult()
    {
        await using var ctx = CreateInMemoryContext(nameof(SyncAllAsync_SingleClient_AggregatesAdapterResult));
        SeedOzonClient(ctx);

        var expectedResult = new OrderSyncResultDto { OrdersCreated = 5, ShipmentsCreated = 3 };
        var adapter = MockAdapter("FBS", expectedResult);

        var svc = new OrderSyncService(ctx, [adapter.Object], NullLogger<OrderSyncService>.Instance);

        var summary = await svc.SyncAllAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);

        summary.Total.OrdersCreated.Should().Be(5);
        summary.Total.ShipmentsCreated.Should().Be(3);
        summary.PerClient.Should().HaveCount(1);
        summary.PerClient[0].Mode.Should().Be("FBS");
    }

    [Fact]
    public async Task SyncAllAsync_TwoClients_AggregatesBothResults()
    {
        await using var ctx = CreateInMemoryContext(nameof(SyncAllAsync_TwoClients_AggregatesBothResults));

        var ozonType = new MarketplaceClientType { Id = Guid.NewGuid(), Name = "Ozon" };
        var mode = new MarketplaceClientMode { Id = Guid.NewGuid(), Name = "FBS" };
        var client1 = new MarketplaceClient { Id = Guid.NewGuid(), Name = "Client A", ApiId = "1", ClientTypeId = ozonType.Id, ClientType = ozonType, ModeId = mode.Id, Mode = mode };
        var client2 = new MarketplaceClient { Id = Guid.NewGuid(), Name = "Client B", ApiId = "2", ClientTypeId = ozonType.Id, ClientType = ozonType, ModeId = mode.Id, Mode = mode };

        ctx.MarketplaceClientTypes!.Add(ozonType);
        ctx.MarketplaceClientModes!.Add(mode);
        ctx.MarketplaceClients!.AddRange(client1, client2);
        ctx.SuppressAudit = true;
        ctx.SaveChanges();

        var adapter = new Mock<IOrderAdapter>();
        adapter.Setup(a => a.ClientModeName).Returns("FBS");
        adapter.Setup(a => a.SyncAsync(It.IsAny<MarketplaceClient>(), It.IsAny<DateTime>(),
                It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderSyncResultDto { OrdersCreated = 2 });

        var svc = new OrderSyncService(ctx, [adapter.Object], NullLogger<OrderSyncService>.Instance);
        var summary = await svc.SyncAllAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);

        summary.Total.OrdersCreated.Should().Be(4, "two clients × 2 orders each");
        summary.PerClient.Should().HaveCount(2);
    }

    [Fact]
    public async Task SyncAllAsync_AdapterThrows_SkipsClientAndReturnsPartialResult()
    {
        await using var ctx = CreateInMemoryContext(nameof(SyncAllAsync_AdapterThrows_SkipsClientAndReturnsPartialResult));

        var ozonType = new MarketplaceClientType { Id = Guid.NewGuid(), Name = "Ozon" };
        var mode = new MarketplaceClientMode { Id = Guid.NewGuid(), Name = "FBS" };
        var client1 = new MarketplaceClient { Id = Guid.NewGuid(), Name = "Good", ApiId = "1", ClientTypeId = ozonType.Id, ClientType = ozonType, ModeId = mode.Id, Mode = mode };
        var client2 = new MarketplaceClient { Id = Guid.NewGuid(), Name = "Bad", ApiId = "2", ClientTypeId = ozonType.Id, ClientType = ozonType, ModeId = mode.Id, Mode = mode };

        ctx.MarketplaceClientTypes!.Add(ozonType);
        ctx.MarketplaceClientModes!.Add(mode);
        ctx.MarketplaceClients!.AddRange(client1, client2);
        ctx.SuppressAudit = true;
        ctx.SaveChanges();

        var callCount = 0;
        var adapter = new Mock<IOrderAdapter>();
        adapter.Setup(a => a.ClientModeName).Returns("FBS");
        adapter.Setup(a => a.SyncAsync(It.IsAny<MarketplaceClient>(), It.IsAny<DateTime>(),
                It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 1)
                    throw new InvalidOperationException("API error");
                return new OrderSyncResultDto { OrdersCreated = 3 };
            });

        var svc = new OrderSyncService(ctx, [adapter.Object], NullLogger<OrderSyncService>.Instance);

        var act = () => svc.SyncAllAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);

        await act.Should().NotThrowAsync("errors per client must be swallowed");
        var summary = await svc.SyncAllAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);
        summary.Total.OrdersCreated.Should().BeGreaterThan(0, "at least one client should succeed");
    }

    [Fact]
    public async Task SyncAllAsync_NoMatchingClients_ReturnsEmptySummary()
    {
        await using var ctx = CreateInMemoryContext(nameof(SyncAllAsync_NoMatchingClients_ReturnsEmptySummary));

        // Seed a non-Ozon client
        var type = new MarketplaceClientType { Id = Guid.NewGuid(), Name = "WB" };
        var mode = new MarketplaceClientMode { Id = Guid.NewGuid(), Name = "FBS" };
        ctx.MarketplaceClientTypes!.Add(type);
        ctx.MarketplaceClientModes!.Add(mode);
        ctx.MarketplaceClients!.Add(new MarketplaceClient { Id = Guid.NewGuid(), Name = "WB Client", ApiId = "1", ClientTypeId = type.Id, ClientType = type, ModeId = mode.Id, Mode = mode });
        ctx.SuppressAudit = true;
        ctx.SaveChanges();

        var adapter = MockAdapter("FBS", new OrderSyncResultDto { OrdersCreated = 99 });
        var svc = new OrderSyncService(ctx, [adapter.Object], NullLogger<OrderSyncService>.Instance);

        var summary = await svc.SyncAllAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);

        summary.Total.OrdersCreated.Should().Be(0);
        summary.PerClient.Should().BeEmpty();
    }
}


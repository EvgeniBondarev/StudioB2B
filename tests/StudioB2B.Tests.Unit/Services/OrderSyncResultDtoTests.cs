using FluentAssertions;
using StudioB2B.Shared;
using Xunit;

namespace StudioB2B.Tests.Unit.Services;

public class OrderSyncResultDtoTests
{
    private static OrderSyncResultDto Make(
        int shipmentsCreated = 0, int shipmentsUpdated = 0, int shipmentsUntouched = 0,
        int ordersCreated = 0, int ordersUpdated = 0, int ordersUntouched = 0,
        int ordersSelected = 0, int ordersSkipped = 0) => new()
    {
        ShipmentsCreated = shipmentsCreated,
        ShipmentsUpdated = shipmentsUpdated,
        ShipmentsUntouched = shipmentsUntouched,
        OrdersCreated = ordersCreated,
        OrdersUpdated = ordersUpdated,
        OrdersUntouched = ordersUntouched,
        OrdersSelectedForUpdate = ordersSelected,
        OrdersSkipped = ordersSkipped
    };

    [Fact]
    public void Add_AccumulatesAllCounters()
    {
        var total = Make(shipmentsCreated: 2, shipmentsUpdated: 3, ordersCreated: 5, ordersUpdated: 1);
        var other = Make(shipmentsCreated: 1, shipmentsUpdated: 4, ordersCreated: 2, ordersUpdated: 7);

        total.Add(other);

        total.ShipmentsCreated.Should().Be(3);
        total.ShipmentsUpdated.Should().Be(7);
        total.OrdersCreated.Should().Be(7);
        total.OrdersUpdated.Should().Be(8);
    }

    [Fact]
    public void Add_WithZeroValues_LeavesCountersUnchanged()
    {
        var total = Make(shipmentsCreated: 5, ordersCreated: 10);

        total.Add(Make());

        total.ShipmentsCreated.Should().Be(5);
        total.OrdersCreated.Should().Be(10);
    }

    [Fact]
    public void Add_AccumulatesSkippedAndSelectedCounters()
    {
        var total = Make(ordersSelected: 10, ordersSkipped: 2);

        total.Add(Make(ordersSelected: 5, ordersSkipped: 3));

        total.OrdersSelectedForUpdate.Should().Be(15);
        total.OrdersSkipped.Should().Be(5);
    }

    [Fact]
    public void Add_AccumulatesUntouchedCounters()
    {
        var total = Make(shipmentsUntouched: 4, ordersUntouched: 6);

        total.Add(Make(shipmentsUntouched: 1, ordersUntouched: 2));

        total.ShipmentsUntouched.Should().Be(5);
        total.OrdersUntouched.Should().Be(8);
    }

    [Fact]
    public void Add_MultipleClients_SumIsCorrect()
    {
        var summary = new OrderSyncSummaryDto();

        summary.Total.Add(Make(ordersCreated: 3, ordersUpdated: 1));
        summary.Total.Add(Make(ordersCreated: 2, ordersUpdated: 4));
        summary.Total.Add(Make(ordersCreated: 0, ordersUpdated: 2));

        summary.Total.OrdersCreated.Should().Be(5);
        summary.Total.OrdersUpdated.Should().Be(7);
    }
}


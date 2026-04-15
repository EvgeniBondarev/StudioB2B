using FluentAssertions;
using StudioB2B.Domain.Constants;
using StudioB2B.Shared;
using Xunit;

namespace StudioB2B.Tests.Unit.Services;

public class OrderTransactionFieldRegistryTests
{
    [Fact]
    public void Get_KnownPath_ReturnsDescriptor()
    {
        var result = OrderTransactionFieldRegistry.Get("Order.StatusId");

        result.Should().NotBeNull();
        result.EntityPath.Should().Be("Order.StatusId");
        result.DisplayName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Get_UnknownPath_ReturnsNull()
        => OrderTransactionFieldRegistry.Get("Unknown.Field").Should().BeNull();

    [Fact]
    public void Get_NullPath_ReturnsNull()
        => OrderTransactionFieldRegistry.Get(null).Should().BeNull();

    [Fact]
    public void Get_CaseInsensitive_ReturnsDescriptor()
    {
        var lower = OrderTransactionFieldRegistry.Get("order.statusid");
        var upper = OrderTransactionFieldRegistry.Get("ORDER.STATUSID");

        lower.Should().NotBeNull();
        upper.Should().NotBeNull();
        lower.EntityPath.Should().Be(upper.EntityPath);
    }

    [Fact]
    public void IsValid_KnownPath_ReturnsTrue()
        => OrderTransactionFieldRegistry.IsValid("Shipment.PostingNumber").Should().BeTrue();

    [Fact]
    public void IsValid_UnknownPath_ReturnsFalse()
        => OrderTransactionFieldRegistry.IsValid("Nonexistent.Path").Should().BeFalse();

    [Fact]
    public void IsValid_EmptyPath_ReturnsFalse()
        => OrderTransactionFieldRegistry.IsValid("").Should().BeFalse();

    [Fact]
    public void All_IsNotEmpty()
        => OrderTransactionFieldRegistry.All.Should().NotBeEmpty();

    [Theory]
    [InlineData("Order.StatusId")]
    [InlineData("Shipment.PostingNumber")]
    [InlineData("Shipment.TrackingNumber")]
    [InlineData("OrderProductInfo.ProductId")]
    [InlineData("Recipient.Name")]
    [InlineData("WarehouseInfo.SenderWarehouseId")]
    public void All_ContainsExpectedPaths(string path)
        => OrderTransactionFieldRegistry.All.Should().Contain(d => d.EntityPath == path);

    [Fact]
    public void Get_FieldWithReferenceType_HasCorrectReferenceType()
    {
        var field = OrderTransactionFieldRegistry.Get("Order.StatusId");

        field.Should().NotBeNull();
        field.ReferenceType.Should().Be(FieldReferenceTypeEnum.OrderStatus);
    }

    [Fact]
    public void Get_StringField_HasStringValueType()
    {
        var field = OrderTransactionFieldRegistry.Get("Shipment.PostingNumber");

        field.Should().NotBeNull();
        field.ValueType.Should().Be(TransactionFieldValueTypeEnum.String);
    }
}

using System.Reflection;
using FluentAssertions;
using StudioB2B.Domain.Constants;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Services.Order;
using Xunit;

namespace StudioB2B.Tests.Unit.Services;

/// <summary>
/// Tests for the private static logic inside OrderTransactionService:
/// ApplyFieldValue, ValidateRequiredPriceRules, ValidateRequiredFieldRules, IsFieldValueEmpty.
/// All methods are accessed via reflection.
/// </summary>
public class OrderTransactionEngineTests
{
    private static readonly Type SvcType = typeof(OrderTransactionService);

    private static bool ApplyFieldValue(OrderEntity order, string path, string? value,
        TransactionFieldValueTypeEnum valueType)
    {
        var m = SvcType.GetMethod("ApplyFieldValue", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (bool)m.Invoke(null, [order, path, value, valueType])!;
    }

    private static List<string> ValidateRequiredPriceRules(OrderEntity order,
        IEnumerable<OrderTransactionRule> rules, IReadOnlyDictionary<Guid, decimal> values)
    {
        var m = SvcType.GetMethod("ValidateRequiredPriceRules", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (List<string>)m.Invoke(null, [order, rules, values])!;
    }

    private static List<string> ValidateRequiredFieldRules(
        IEnumerable<OrderTransactionFieldRule> fieldRules, IReadOnlyDictionary<Guid, string> values)
    {
        var m = SvcType.GetMethod("ValidateRequiredFieldRules", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (List<string>)m.Invoke(null, [fieldRules, values])!;
    }

    private static bool IsFieldValueEmpty(string? value, TransactionFieldValueTypeEnum type)
    {
        var m = SvcType.GetMethod("IsFieldValueEmpty", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (bool)m.Invoke(null, [value, type])!;
    }

    private static OrderEntity NewOrder() => new()
    {
        Id = Guid.NewGuid(),
        Quantity = 1,
        Shipment = new Shipment { Id = Guid.NewGuid(), PostingNumber = "ORIG-001" }
    };

    // ApplyFieldValue — Order.Quantity

    [Fact]
    public void ApplyFieldValue_Quantity_NewValue_SetsQuantityAndReturnsTrue()
    {
        var order = NewOrder();
        var result = ApplyFieldValue(order, "Order.Quantity", "5", TransactionFieldValueTypeEnum.Int);

        result.Should().BeTrue();
        order.Quantity.Should().Be(5);
    }

    [Fact]
    public void ApplyFieldValue_Quantity_SameValue_ReturnsFalse()
    {
        var order = NewOrder();
        order.Quantity = 3;
        var result = ApplyFieldValue(order, "Order.Quantity", "3", TransactionFieldValueTypeEnum.Int);

        result.Should().BeFalse("no change should return false");
    }

    [Fact]
    public void ApplyFieldValue_Quantity_InvalidInt_ReturnsFalse()
    {
        var order = NewOrder();
        var result = ApplyFieldValue(order, "Order.Quantity", "not-a-number", TransactionFieldValueTypeEnum.Int);

        result.Should().BeFalse();
    }

    // ApplyFieldValue — Order.StatusId

    [Fact]
    public void ApplyFieldValue_StatusId_NewGuid_SetsStatusIdAndReturnsTrue()
    {
        var order = NewOrder();
        var newId = Guid.NewGuid();
        var result = ApplyFieldValue(order, "Order.StatusId", newId.ToString(), TransactionFieldValueTypeEnum.Guid);

        result.Should().BeTrue();
        order.StatusId.Should().Be(newId);
    }

    // ApplyFieldValue — Shipment.PostingNumber

    [Fact]
    public void ApplyFieldValue_ShipmentPostingNumber_NewValue_SetsAndReturnsTrue()
    {
        var order = NewOrder();
        var result = ApplyFieldValue(order, "Shipment.PostingNumber", "NEW-123", TransactionFieldValueTypeEnum.String);

        result.Should().BeTrue();
        order.Shipment.PostingNumber.Should().Be("NEW-123");
    }

    [Fact]
    public void ApplyFieldValue_ShipmentPostingNumber_WhenNoShipment_ReturnsFalse()
    {
        var order = new OrderEntity { Id = Guid.NewGuid(), Quantity = 1, Shipment = null! };
        var result = ApplyFieldValue(order, "Shipment.PostingNumber", "ABC", TransactionFieldValueTypeEnum.String);

        result.Should().BeFalse();
    }

    [Fact]
    public void ApplyFieldValue_ShipmentTrackingNumber_SetsAndReturnsTrue()
    {
        var order = NewOrder();
        var result = ApplyFieldValue(order, "Shipment.TrackingNumber", "TRK-999", TransactionFieldValueTypeEnum.String);

        result.Should().BeTrue();
        order.Shipment.TrackingNumber.Should().Be("TRK-999");
    }

    [Fact]
    public void ApplyFieldValue_UnknownPath_ReturnsFalse()
    {
        var order = NewOrder();
        var result = ApplyFieldValue(order, "Unknown.Field", "value", TransactionFieldValueTypeEnum.String);

        result.Should().BeFalse();
    }

    // ValidateRequiredPriceRules

    [Fact]
    public void ValidateRequiredPriceRules_RequiredRuleWithNoValue_ReturnsError()
    {
        var order = NewOrder();
        var priceType = new PriceType { Id = Guid.NewGuid(), Name = "Скидка" };
        var rule = new OrderTransactionRule
        {
            Id = Guid.NewGuid(),
            PriceTypeId = priceType.Id,
            PriceType = priceType,
            IsRequired = true,
            ValueSource = TransactionValueSourceEnum.UserInput
        };

        var errors = ValidateRequiredPriceRules(order, [rule], new Dictionary<Guid, decimal>());

        errors.Should().ContainSingle().Which.Should().Be("Скидка");
    }

    [Fact]
    public void ValidateRequiredPriceRules_RequiredRuleWithValue_NoError()
    {
        var order = NewOrder();
        var priceType = new PriceType { Id = Guid.NewGuid(), Name = "Скидка" };
        var ruleId = Guid.NewGuid();
        var rule = new OrderTransactionRule
        {
            Id = ruleId,
            PriceTypeId = priceType.Id,
            PriceType = priceType,
            IsRequired = true,
            ValueSource = TransactionValueSourceEnum.UserInput
        };

        var errors = ValidateRequiredPriceRules(order, [rule], new Dictionary<Guid, decimal> { [ruleId] = 100m });

        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateRequiredPriceRules_NotRequiredRuleWithNoValue_NoError()
    {
        var order = NewOrder();
        var priceType = new PriceType { Id = Guid.NewGuid(), Name = "Скидка" };
        var rule = new OrderTransactionRule
        {
            Id = Guid.NewGuid(),
            PriceType = priceType,
            IsRequired = false,
            ValueSource = TransactionValueSourceEnum.UserInput
        };

        var errors = ValidateRequiredPriceRules(order, [rule], new Dictionary<Guid, decimal>());

        errors.Should().BeEmpty();
    }

    // ValidateRequiredFieldRules

    [Fact]
    public void ValidateRequiredFieldRules_RequiredEmptyValue_ReturnsDisplayName()
    {
        var rule = new OrderTransactionFieldRule
        {
            Id = Guid.NewGuid(),
            EntityPath = "Shipment.TrackingNumber",
            IsRequired = true,
            ValueSource = TransactionFieldValueSourceEnum.UserInput
        };

        var errors = ValidateRequiredFieldRules([rule], new Dictionary<Guid, string>());

        errors.Should().ContainSingle().Which.Should().Be("Трек-номер");
    }

    [Fact]
    public void ValidateRequiredFieldRules_RequiredValidValue_NoError()
    {
        var ruleId = Guid.NewGuid();
        var rule = new OrderTransactionFieldRule
        {
            Id = ruleId,
            EntityPath = "Shipment.TrackingNumber",
            IsRequired = true,
            ValueSource = TransactionFieldValueSourceEnum.UserInput
        };

        var errors = ValidateRequiredFieldRules([rule],
            new Dictionary<Guid, string> { [ruleId] = "TRK-001" });

        errors.Should().BeEmpty();
    }

    // IsFieldValueEmpty

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsFieldValueEmpty_NullOrWhitespace_ReturnsTrue(string? value)
        => IsFieldValueEmpty(value, TransactionFieldValueTypeEnum.String).Should().BeTrue();

    [Fact]
    public void IsFieldValueEmpty_ValidGuid_ReturnsFalse()
        => IsFieldValueEmpty(Guid.NewGuid().ToString(), TransactionFieldValueTypeEnum.Guid).Should().BeFalse();

    [Fact]
    public void IsFieldValueEmpty_EmptyGuid_ReturnsTrue()
        => IsFieldValueEmpty(Guid.Empty.ToString(), TransactionFieldValueTypeEnum.Guid).Should().BeTrue();

    [Fact]
    public void IsFieldValueEmpty_InvalidGuid_ReturnsTrue()
        => IsFieldValueEmpty("not-a-guid", TransactionFieldValueTypeEnum.Guid).Should().BeTrue();

    [Fact]
    public void IsFieldValueEmpty_ValidInt_ReturnsFalse()
        => IsFieldValueEmpty("42", TransactionFieldValueTypeEnum.Int).Should().BeFalse();

    [Fact]
    public void IsFieldValueEmpty_InvalidInt_ReturnsTrue()
        => IsFieldValueEmpty("nope", TransactionFieldValueTypeEnum.Int).Should().BeTrue();

    [Fact]
    public void IsFieldValueEmpty_ValidDecimal_ReturnsFalse()
        => IsFieldValueEmpty("99.99", TransactionFieldValueTypeEnum.Decimal).Should().BeFalse();
}


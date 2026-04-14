using FluentAssertions;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Services;
using Xunit;

namespace StudioB2B.Tests.Unit.Services;

public class CalculationEngineTests
{
    private readonly CalculationEngine _engine = new();

    private static OrderEntity BuildOrder(int quantity, params (string PriceTypeName, decimal Value)[] prices)
    {
        var order = new OrderEntity
        {
            Id = Guid.NewGuid(),
            Quantity = quantity,
            ShipmentId = Guid.NewGuid(),
            Prices = prices.Select(p => new OrderPrice
            {
                Id = Guid.NewGuid(),
                Value = p.Value,
                PriceType = new PriceType { Id = Guid.NewGuid(), Name = p.PriceTypeName }
            }).ToList()
        };
        return order;
    }

    private static CalculationRule Rule(string resultKey, string formula, int sortOrder = 1) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = resultKey,
            ResultKey = resultKey,
            Formula = formula,
            IsActive = true,
            SortOrder = sortOrder
        };

    [Fact]
    public void Calculate_SimpleFormula_ReturnsCorrectResult()
    {
        var order = BuildOrder(2, ("Цена", 100m));
        var rules = new[] { Rule("Комиссия", "Цена * 0.1m") };

        var result = _engine.Calculate(order, rules);

        result["Комиссия"].Should().Be(10m);
    }

    [Fact]
    public void Calculate_UsesQuantityVariable()
    {
        var order = BuildOrder(5, ("Цена", 50m));
        var rules = new[] { Rule("Итого", "Цена * Quantity") };

        var result = _engine.Calculate(order, rules);

        result["Итого"].Should().Be(250m);
    }

    [Fact]
    public void Calculate_ChainedRules_SecondRuleUsesFirstResult()
    {
        var order = BuildOrder(1, ("Цена", 100m));
        var rules = new[]
        {
            Rule("Комиссия", "Цена * 0.1m", sortOrder: 1),
            Rule("Итог", "Цена - Комиссия", sortOrder: 2)
        };

        var result = _engine.Calculate(order, rules);

        result["Комиссия"].Should().Be(10m);
        result["Итог"].Should().Be(90m);
    }

    [Fact]
    public void Calculate_InvalidFormula_ReturnsMinValueAndRecordsError()
    {
        var order = BuildOrder(1, ("Цена", 100m));
        var rules = new[] { Rule("Bad", "UnknownVariable / 0") };

        var result = _engine.Calculate(order, rules);

        result["Bad"].Should().Be(decimal.MinValue);
        _engine.LastErrors.Should().ContainKey("Bad");
    }

    [Fact]
    public void Calculate_EmptyFormula_IsSkipped()
    {
        var order = BuildOrder(1, ("Цена", 100m));
        var rules = new[] { Rule("Empty", "") };

        var result = _engine.Calculate(order, rules);

        result.Should().NotContainKey("Empty");
    }

    [Fact]
    public void GetBaseVariableNames_IncludesQuantityAndSanitizedPriceNames()
    {
        var names = CalculationEngine.GetBaseVariableNames(["Цена до скидки", "Базовая цена"]);

        names.Should().Contain("Quantity");
        names.Should().Contain("ЦенаДоСкидки");
        names.Should().Contain("БазоваяЦена");
    }

    [Fact]
    public void SanitizeKey_MultiWordName_ReturnsCamelCase()
    {
        CalculationEngine.SanitizeKey("Цена до скидки").Should().Be("ЦенаДоСкидки");
    }

    [Fact]
    public void SanitizeKey_EmptyString_ReturnsEmpty()
    {
        CalculationEngine.SanitizeKey("").Should().Be(string.Empty);
        CalculationEngine.SanitizeKey("   ").Should().Be(string.Empty);
    }

    [Fact]
    public void EvaluateFormula_WithContext_ReturnsExpectedValue()
    {
        var context = new Dictionary<string, decimal> { ["x"] = 10m, ["y"] = 3m };

        var result = _engine.EvaluateFormula("x * y", context);

        result.Should().Be(30m);
    }

    [Fact]
    public void ValidateFormula_ValidFormula_ReturnsNull()
    {
        var error = _engine.ValidateFormula("x + y", ["x", "y"]);

        error.Should().BeNull();
    }

    [Fact]
    public void ValidateFormula_InvalidFormula_ReturnsErrorMessage()
    {
        var error = _engine.ValidateFormula("x ++ bad syntax {{", ["x"]);

        error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CalculateWithContext_ChainedContext_Works()
    {
        var initial = new Dictionary<string, decimal> { ["A"] = 10m };
        var rules = new[]
        {
            Rule("B", "A * 2", sortOrder: 1),
            Rule("C", "A + B", sortOrder: 2)
        };

        var result = _engine.CalculateWithContext(initial, rules);

        result["B"].Should().Be(20m);
        result["C"].Should().Be(30m);
    }
}


using FluentAssertions;
using StudioB2B.Domain.Constants;
using StudioB2B.Shared;
using Xunit;

namespace StudioB2B.Tests.Unit.Services;

public class CommunicationPaymentCalculatorTests
{
    private static CommunicationPaymentRateDto MakeRate(
        PaymentMode mode, decimal rate,
        CommunicationTaskType? taskType = null,
        int? min = null, int? max = null, bool active = true) => new()
    {
        Id = Guid.NewGuid(),
        PaymentMode = mode,
        Rate = rate,
        TaskType = taskType,
        MinDurationMinutes = min,
        MaxDurationMinutes = max,
        IsActive = active
    };

    [Fact]
    public void PerTask_ReturnsRate()
    {
        var result = CommunicationPaymentCalculator.ComputeRateContribution(45m, PaymentMode.PerTask, 100m, null);
        result.Should().Be(100m);
    }

    [Fact]
    public void Hourly_NoMax_ProportionalTo60()
    {
        var result = CommunicationPaymentCalculator.ComputeRateContribution(30m, PaymentMode.Hourly, 120m, null);
        result.Should().Be(60m);
    }

    [Fact]
    public void Hourly_WithMax_ProportionalToMax()
    {
        var result = CommunicationPaymentCalculator.ComputeRateContribution(15m, PaymentMode.Hourly, 200m, 30);
        result.Should().Be(50m);
    }

    [Fact]
    public void InactiveRate_IsExcluded()
    {
        var rates = new[] { MakeRate(PaymentMode.PerTask, 100m, active: false) };
        CommunicationPaymentCalculator.ComputeBreakdownLines(60m, CommunicationTaskType.Chat, null, rates)
            .Should().BeEmpty();
    }

    [Fact]
    public void FloorBilling_EffectiveIsMin()
    {
        var rates = new[] { MakeRate(PaymentMode.Hourly, 120m, min: 30) };
        var lines = CommunicationPaymentCalculator.ComputeBreakdownLines(10m, CommunicationTaskType.Chat, null, rates);
        lines.Should().HaveCount(1);
        lines[0].Amount.Should().Be(60m); // 120 * (30/60)
    }

    [Fact]
    public void CeilingBilling_EffectiveIsMax()
    {
        var rates = new[] { MakeRate(PaymentMode.Hourly, 200m, max: 60) };
        var lines = CommunicationPaymentCalculator.ComputeBreakdownLines(120m, CommunicationTaskType.Chat, null, rates);
        lines.Should().HaveCount(1);
        lines[0].Amount.Should().Be(200m); // 200 * (60/60)
    }

    [Fact]
    public void SpecificTaskType_ExcludesGeneral()
    {
        var rates = new[]
        {
            MakeRate(PaymentMode.PerTask, 50m, taskType: null),
            MakeRate(PaymentMode.PerTask, 80m, taskType: CommunicationTaskType.Chat)
        };
        var lines = CommunicationPaymentCalculator.ComputeBreakdownLines(30m, CommunicationTaskType.Chat, null, rates);
        lines.Should().HaveCount(1);
        lines[0].Amount.Should().Be(80m);
    }

    [Fact]
    public void WrongTaskType_IsExcluded()
    {
        var rates = new[] { MakeRate(PaymentMode.PerTask, 100m, taskType: CommunicationTaskType.Question) };
        CommunicationPaymentCalculator.ComputeBreakdownLines(30m, CommunicationTaskType.Chat, null, rates)
            .Should().BeEmpty();
    }
}

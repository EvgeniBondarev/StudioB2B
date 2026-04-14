using FluentAssertions;
using StudioB2B.Infrastructure.Helpers;
using Xunit;

namespace StudioB2B.Tests.Unit.Services;

public class ScheduleCronBuilderTests
{
    [Theory]
    [InlineData("*/15 * * * *", "Каждые 15 мин.")]
    [InlineData("*/5 * * * *",  "Каждые 5 мин.")]
    public void EveryNMinutes(string cron, string expected)
        => ScheduleCronBuilder.DescribeCron(cron).Should().Be(expected);

    [Theory]
    [InlineData("0 */2 * * *",  "Каждые 2 ч. в :00")]
    [InlineData("30 */6 * * *", "Каждые 6 ч. в :30")]
    public void EveryNHours(string cron, string expected)
        => ScheduleCronBuilder.DescribeCron(cron).Should().Be(expected);

    [Theory]
    [InlineData("0 9 * * *",  "Ежедневно в 09:00")]
    [InlineData("30 14 * * *", "Ежедневно в 14:30")]
    public void Daily(string cron, string expected)
        => ScheduleCronBuilder.DescribeCron(cron).Should().Be(expected);

    [Theory]
    [InlineData("0 9 */2 * *", "Каждые 2 дн. в 09:00")]
    public void EveryNDays(string cron, string expected)
        => ScheduleCronBuilder.DescribeCron(cron).Should().Be(expected);

    [Fact]
    public void WeekdaysMonToFri() => ScheduleCronBuilder.DescribeCron("0 9 * * 1-5").Should().Be("Пн–Пт в 09:00");

    [Fact]
    public void Weekend() => ScheduleCronBuilder.DescribeCron("0 10 * * 0,6").Should().Be("Сб, Вс в 10:00");

    [Fact]
    public void DayOfMonth() => ScheduleCronBuilder.DescribeCron("0 8 15 * *").Should().Be("15-го числа в 08:00");

    [Fact]
    public void Null_ReturnsDash() => ScheduleCronBuilder.DescribeCron(null).Should().Be("—");

    [Fact]
    public void Empty_ReturnsDash() => ScheduleCronBuilder.DescribeCron("").Should().Be("—");

    [Fact]
    public void InvalidParts_ReturnsCronPrefix()
        => ScheduleCronBuilder.DescribeCron("bad cron").Should().StartWith("Cron:");
}

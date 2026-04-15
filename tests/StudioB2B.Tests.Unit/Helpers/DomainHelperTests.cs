using FluentAssertions;
using StudioB2B.Infrastructure.Helpers;
using Xunit;

namespace StudioB2B.Tests.Unit.Helpers;

public class DomainHelperTests
{
    [Fact]
    public void Normalize_EmptyString_ReturnsEmpty()
        => DomainHelper.Normalize("").Should().Be("");

    [Theory]
    [InlineData("https://example.com/", "example.com")]
    [InlineData("https://example.com", "example.com")]
    [InlineData("http://example.com/", "example.com")]
    [InlineData("http://example.com", "example.com")]
    public void Normalize_RemovesProtocolAndTrailingSlash(string input, string expected)
        => DomainHelper.Normalize(input).Should().Be(expected);

    [Fact]
    public void Normalize_NoProtocol_ReturnsAsIs()
        => DomainHelper.Normalize("example.com").Should().Be("example.com");

    [Fact]
    public void Normalize_WithPath_KeepsPathRemovesTrailingSlash()
        => DomainHelper.Normalize("https://example.com/tenant/").Should().Be("example.com/tenant");

    [Fact]
    public void Normalize_UpperCaseProtocol_Handled()
        => DomainHelper.Normalize("HTTPS://example.com/").Should().Be("example.com");

    [Fact]
    public void Normalize_TrailingSlashOnly_Stripped()
        => DomainHelper.Normalize("example.com/").Should().Be("example.com");
}


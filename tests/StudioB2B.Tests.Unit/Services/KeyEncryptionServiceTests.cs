using FluentAssertions;
using Microsoft.Extensions.Options;
using StudioB2B.Domain.Options;
using StudioB2B.Infrastructure.Services;
using Xunit;

namespace StudioB2B.Tests.Unit.Services;

public class KeyEncryptionServiceTests
{
    private static KeyEncryptionService Create(string? base64Key = null)
    {
        var opts = Options.Create(new EncryptionOptions { Key = base64Key ?? "" });
        return new KeyEncryptionService(opts);
    }

    [Theory]
    [InlineData("hello")]
    [InlineData("some-api-key-12345")]
    public void EncryptDecrypt_RoundTrip(string plain)
    {
        var svc = Create(Convert.ToBase64String(new byte[32]));
        svc.Decrypt(svc.Encrypt(plain)).Should().Be(plain);
    }

    [Fact]
    public void Encrypt_DifferentCipherEachCall()
    {
        var svc = Create(Convert.ToBase64String(new byte[32]));
        svc.Encrypt("x").Should().NotBe(svc.Encrypt("x"));
    }

    [Fact]
    public void Encrypt_Empty_ReturnsEmpty() => Create().Encrypt("").Should().Be("");

    [Fact]
    public void Decrypt_Empty_ReturnsEmpty() => Create().Decrypt("").Should().Be("");

    [Fact]
    public void Decrypt_PlainText_ReturnsSameValue()
    {
        var plain = "plain-not-encrypted";
        Create().Decrypt(plain).Should().Be(plain);
    }

    [Fact]
    public void FallbackKey_RoundTrip()
    {
        var svc = Create();
        svc.Decrypt(svc.Encrypt("api-key")).Should().Be("api-key");
    }

    [Fact]
    public void InvalidConfigKey_UsesFallback()
    {
        var svc = Create("!!!not-base64!!!");
        svc.Decrypt(svc.Encrypt("data")).Should().Be("data");
    }
}

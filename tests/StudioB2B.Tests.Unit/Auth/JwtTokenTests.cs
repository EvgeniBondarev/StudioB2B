using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using StudioB2B.Domain.Options;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Web.Controllers;
using Xunit;

namespace StudioB2B.Tests.Unit.Auth;

/// <summary>
/// Verifies that AccountController.GenerateJwtToken embeds the correct claims.
/// The private method is exercised via reflection so no HTTP pipeline is needed.
/// </summary>
public class JwtTokenTests
{
    private static readonly string Secret = new string('x', 64); // 64-char key → valid for HMAC-SHA256

    private static AccountController CreateController()
    {
        var jwtOptions = Options.Create(new JwtOptions
        {
            Secret = Secret,
            Issuer = "test-issuer",
            Audience = "test-audience",
            ExpiresMinutes = 60
        });

        return new AccountController(
            Mock.Of<IAccountService>(),
            Mock.Of<ITenantProvider>(),
            jwtOptions,
            NullLogger<AccountController>.Instance);
    }

    private static string GenerateToken(AccountController controller, Guid userId, string email,
        bool isFullAccess, IEnumerable<string> roles)
    {
        var method = typeof(AccountController)
            .GetMethod("GenerateJwtToken", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (string)method.Invoke(controller, [userId, email, isFullAccess, roles])!;
    }

    private static JwtSecurityToken ParseToken(string tokenString)
        => new JwtSecurityTokenHandler().ReadJwtToken(tokenString);

    [Fact]
    public void GenerateJwtToken_ContainsSubClaim()
    {
        var userId = Guid.NewGuid();
        var token = GenerateToken(CreateController(), userId, "user@test.com", false, []);
        var jwt = ParseToken(token);

        jwt.Subject.Should().Be(userId.ToString());
    }

    [Fact]
    public void GenerateJwtToken_ContainsEmailClaim()
    {
        var token = GenerateToken(CreateController(), Guid.NewGuid(), "user@test.com", false, []);
        var jwt = ParseToken(token);

        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "user@test.com");
    }

    [Fact]
    public void GenerateJwtToken_ContainsRoleClaims()
    {
        var roles = new[] { "OrdersView", "UsersView" };
        var token = GenerateToken(CreateController(), Guid.NewGuid(), "user@test.com", false, roles);
        var jwt = ParseToken(token);

        var roleClaims = jwt.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        roleClaims.Should().Contain("OrdersView");
        roleClaims.Should().Contain("UsersView");
    }

    [Fact]
    public void GenerateJwtToken_WhenFullAccess_ContainsFullAccessClaim()
    {
        var token = GenerateToken(CreateController(), Guid.NewGuid(), "admin@test.com", isFullAccess: true, []);
        var jwt = ParseToken(token);

        jwt.Claims.Should().Contain(c => c.Type == "full_access" && c.Value == "true");
    }

    [Fact]
    public void GenerateJwtToken_WhenNotFullAccess_NoFullAccessClaim()
    {
        var token = GenerateToken(CreateController(), Guid.NewGuid(), "user@test.com", isFullAccess: false, []);
        var jwt = ParseToken(token);

        jwt.Claims.Should().NotContain(c => c.Type == "full_access");
    }

    [Fact]
    public void GenerateJwtToken_ContainsIssuerAndAudience()
    {
        var token = GenerateToken(CreateController(), Guid.NewGuid(), "user@test.com", false, []);
        var jwt = ParseToken(token);

        jwt.Issuer.Should().Be("test-issuer");
        jwt.Audiences.Should().Contain("test-audience");
    }
}


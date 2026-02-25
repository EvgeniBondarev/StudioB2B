using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StudioB2B.Domain.Options;

namespace StudioB2B.Web.Services;

/// <summary>
/// Blazor Server AuthenticationStateProvider.
/// Читает JWT из HttpOnly-куки auth_token и возвращает ClaimsPrincipal.
/// Вызывается Blazor при каждом рендере компонентов с AuthorizeView.
/// </summary>
public class CookieAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly TokenValidationParameters _validationParams;

    public CookieAuthenticationStateProvider(
        IHttpContextAccessor httpContextAccessor,
        IOptions<JwtOptions> jwtOptions)
    {
        _httpContextAccessor = httpContextAccessor;

        var jwt = jwtOptions.Value;
        _validationParams = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
            ClockSkew = TimeSpan.Zero
        };
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = _httpContextAccessor.HttpContext?.Request.Cookies["auth_token"];

        if (string.IsNullOrWhiteSpace(token))
            return Task.FromResult(Anonymous());

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, _validationParams, out _);
            return Task.FromResult(new AuthenticationState(principal));
        }
        catch
        {
            return Task.FromResult(Anonymous());
        }
    }

    private static AuthenticationState Anonymous() =>
        new(new ClaimsPrincipal(new ClaimsIdentity()));
}


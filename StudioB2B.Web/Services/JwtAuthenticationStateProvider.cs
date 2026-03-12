using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace StudioB2B.Web.Services;

/// <summary>
/// AuthenticationStateProvider на основе JWT, хранящегося в localStorage.
/// </summary>
public class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    private const string TokenKey = "auth_token";
    private const string MasterTokenKey = "master_auth_token";
    private readonly IJSRuntime _js;

    public JwtAuthenticationStateProvider(IJSRuntime js)
    {
        _js = js;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await _js.InvokeAsync<string?>("localStorage.getItem", TokenKey);
            if (string.IsNullOrWhiteSpace(token))
                return Anonymous();

            var principal = ParseToken(token);
            if (principal is null)
                return Anonymous();

            return new AuthenticationState(principal);
        }
        catch
        {
            return Anonymous();
        }
    }

    public async Task LoginAsync(string token)
    {
        await _js.InvokeVoidAsync("localStorage.setItem", TokenKey, token);
        var principal = ParseToken(token) ?? new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(principal)));
    }

    public async Task LogoutAsync()
    {
        await _js.InvokeVoidAsync("localStorage.removeItem", TokenKey);
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous()));
    }

    public async Task<string?> GetTokenAsync()
    {
        try { return await _js.InvokeAsync<string?>("localStorage.getItem", TokenKey); }
        catch { return null; }
    }

    public async Task MasterLoginAsync(string token)
    {
        await _js.InvokeVoidAsync("localStorage.setItem", MasterTokenKey, token);
    }

    public async Task MasterLogoutAsync()
    {
        await _js.InvokeVoidAsync("localStorage.removeItem", MasterTokenKey);
    }

    public async Task<bool> IsMasterAuthenticatedAsync()
    {
        try
        {
            var token = await _js.InvokeAsync<string?>("localStorage.getItem", MasterTokenKey);
            if (string.IsNullOrWhiteSpace(token)) return false;
            var principal = ParseToken(token);
            return principal is not null;
        }
        catch { return false; }
    }

    private static AuthenticationState Anonymous()
        => new(new ClaimsPrincipal(new ClaimsIdentity()));

    private static ClaimsPrincipal? ParseToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token)) return null;

            var jwt = handler.ReadJwtToken(token);

            // Проверяем срок действия
            if (jwt.ValidTo < DateTime.UtcNow) return null;

            var identity = new ClaimsIdentity(jwt.Claims, "jwt");
            return new ClaimsPrincipal(identity);
        }
        catch
        {
            return null;
        }
    }
}


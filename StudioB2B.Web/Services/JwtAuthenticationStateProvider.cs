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

    /// <summary>
    /// Читает claims из master JWT: email, given_name, family_name, middle_name, roles.
    /// </summary>
    public async Task<MasterUserInfo?> GetMasterUserInfoAsync()
    {
        try
        {
            var token = await _js.InvokeAsync<string?>("localStorage.getItem", MasterTokenKey);
            if (string.IsNullOrWhiteSpace(token)) return null;

            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token)) return null;

            var jwt = handler.ReadJwtToken(token);
            if (jwt.ValidTo < DateTime.UtcNow) return null;

            var email = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email)?.Value ?? "";
            var firstName = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.GivenName)?.Value ?? "";
            var lastName = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.FamilyName)?.Value ?? "";
            var middleName = jwt.Claims.FirstOrDefault(c => c.Type == "middle_name")?.Value;
            var roles = jwt.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
            var subClaim = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
            var userId = subClaim is not null && Guid.TryParse(subClaim, out var parsed) ? parsed : Guid.Empty;

            return new MasterUserInfo(userId, email, firstName, lastName, middleName, roles);
        }
        catch { return null; }
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


using Microsoft.AspNetCore.Http;

namespace StudioB2B.Infrastructure.MultiTenancy.Extensions;

public static class CookieExtensions
{
    private const string LastTenantCookieName = "LastTenant";
    private const int CookieExpirationDays = 30;

    public static void AppendLastTenantCookie(
        this HttpResponse response,
        string subdomain,
        string masterDomain)
    {
        var cookieOptions = CreateLastTenantCookieOptions(masterDomain);

        response.Cookies.Append(LastTenantCookieName, subdomain, cookieOptions);
    }

    public static void DeleteLastTenantCookie(
        this HttpResponse response,
        string masterDomain)
    {
        var cookieOptions = CreateBaseCookieOptions(masterDomain);

        response.Cookies.Delete(LastTenantCookieName, cookieOptions);
    }

    public static string? GetLastTenant(this HttpRequest request)
    {
        return request.Cookies.TryGetValue(LastTenantCookieName, out var lastTenant)
                   ? lastTenant
                   : null;
    }

    private static CookieOptions CreateLastTenantCookieOptions(string masterDomain)
    {
        var options = CreateBaseCookieOptions(masterDomain);

        options.HttpOnly = true;
        options.Secure = true;
        options.SameSite = SameSiteMode.Lax;
        options.MaxAge = TimeSpan.FromDays(CookieExpirationDays);

        return options;
    }

    private static CookieOptions CreateBaseCookieOptions(string masterDomain)
    {
        return new CookieOptions
               {
                   Domain = masterDomain,
                   Path = "/"
               };
    }
}

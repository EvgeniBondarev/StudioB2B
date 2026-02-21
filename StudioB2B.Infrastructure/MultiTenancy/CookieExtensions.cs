using Microsoft.AspNetCore.Http;

namespace StudioB2B.Infrastructure.MultiTenancy;


public static class CookieExtensions
{
    public static void AppendLastTenantCookie(this HttpResponse response, string subdomain, string masterDomain)
    {
        response.Cookies.Append("LastTenant", subdomain, new CookieOptions
                                                         {
                                                             Domain = masterDomain,
                                                             Path = "/",
                                                             HttpOnly = true,
                                                             Secure = true,
                                                             SameSite = SameSiteMode.Lax,
                                                             MaxAge = TimeSpan.FromDays(30)
                                                         });
    }

    public static void DeleteLastTenantCookie(this HttpResponse response, string masterDomain)
    {
        response.Cookies.Delete("LastTenant", new CookieOptions
                                              {
                                                  Domain = masterDomain,
                                                  Path = "/"
                                              });
    }

    public static string? GetLastTenant(this HttpRequest request)
    {
        return request.Cookies["LastTenant"];
    }
}

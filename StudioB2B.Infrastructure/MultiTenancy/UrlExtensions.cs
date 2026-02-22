namespace StudioB2B.Infrastructure.MultiTenancy;

public static class UrlExtensions
{
    public static string GetTenantUrl(this string subdomain, string masterDomain, string path = "/")
    {
        masterDomain = masterDomain.TrimStart("https://".ToCharArray())
            .TrimStart("http://".ToCharArray());

        return $"https://{subdomain}.{masterDomain}{path}";
    }
}

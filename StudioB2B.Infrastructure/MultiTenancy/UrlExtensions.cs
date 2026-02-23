namespace StudioB2B.Infrastructure.MultiTenancy;

public static class UrlExtensions
{
    public static string GetTenantUrl(this string subdomain, string masterDomain, string path = "/")
    {
        if (string.IsNullOrEmpty(masterDomain))
            throw new ArgumentException("Master domain must be provided", nameof(masterDomain));

        if (masterDomain.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            masterDomain = masterDomain.Substring("https://".Length);
        else if (masterDomain.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            masterDomain = masterDomain.Substring("http://".Length);

        masterDomain = masterDomain.TrimEnd('/');

        if (string.IsNullOrEmpty(path))
        {
            path = "/";
        }
        else
        {
            if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                path = "/";
            }
            else if (!path.StartsWith("/"))
            {
                path = "/" + path;
            }
        }

        return $"https://{subdomain}.{masterDomain}{path}";
    }
}

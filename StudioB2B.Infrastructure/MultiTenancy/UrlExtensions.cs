namespace StudioB2B.Infrastructure.MultiTenancy;

public static class UrlExtensions
{
    public static string GetTenantUrl(this string subdomain, string masterDomain, string path = "/")
    {
        if (string.IsNullOrEmpty(masterDomain))
            throw new ArgumentException("Master domain must be provided", nameof(masterDomain));

        // remove any leading scheme (http:// or https://) rather than trimming characters individually
        if (masterDomain.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            masterDomain = masterDomain.Substring("https://".Length);
        else if (masterDomain.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            masterDomain = masterDomain.Substring("http://".Length);

        // drop any trailing slash that might have been included in configuration
        masterDomain = masterDomain.TrimEnd('/');

        // ensure path is sane: always starts with a slash and doesn't contain a scheme
        if (string.IsNullOrEmpty(path))
        {
            path = "/";
        }
        else
        {
            // if someone passed an absolute URL by mistake, ignore it and use root
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

namespace StudioB2B.Infrastructure.Helpers;

public static class UrlExtensions
{
    private const string HttpsScheme = "https://";
    private const string HttpScheme = "http://";
    private const char PathSeparator = '/';

    public static string GetTenantUrl(this string subdomain, string masterDomain, string path = "/")
    {
        if (string.IsNullOrWhiteSpace(masterDomain))
        {
            throw new ArgumentException("Master domain must be provided", nameof(masterDomain));
        }

        var normalizedDomain = DomainHelper.Normalize(masterDomain);
        var sanitizedPath = SanitizePath(path);

        return $"{HttpsScheme}{subdomain}.{normalizedDomain}{sanitizedPath}";
    }

    private static string SanitizePath(string? path)
    {
        if (string.IsNullOrEmpty(path) ||
            path.StartsWith(HttpScheme, StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith(HttpsScheme, StringComparison.OrdinalIgnoreCase))
            return PathSeparator.ToString();

        return path.StartsWith(PathSeparator) ? path : $"{PathSeparator}{path}";
    }
}

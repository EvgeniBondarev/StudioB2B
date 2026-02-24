using StudioB2B.Infrastructure.MultiTenancy.Resolution;

namespace StudioB2B.Infrastructure.MultiTenancy;

public static class UrlExtensions
{
    private const string HttpsScheme = "https://";
    private const string HttpScheme = "http://";
    private const char PathSeparator = '/';

    public static string GetTenantUrl(this string subdomain, string masterDomain, string path = "/")
    {
        ValidateMasterDomain(masterDomain);

        var normalizedDomain = DomainHelper.Normalize(masterDomain);
        var sanitizedPath = SanitizePath(path);

        return BuildTenantUrl(subdomain, normalizedDomain, sanitizedPath);
    }

    private static void ValidateMasterDomain(string masterDomain)
    {
        if (string.IsNullOrWhiteSpace(masterDomain))
        {
            throw new ArgumentException("Master domain must be provided", nameof(masterDomain));
        }
    }

    private static string SanitizePath(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return PathSeparator.ToString();

        if (IsAbsoluteUrl(path))
            return PathSeparator.ToString();

        return EnsureLeadingSlash(path);
    }

    private static bool IsAbsoluteUrl(string path)
    {
        return path.StartsWith(HttpScheme, StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith(HttpsScheme, StringComparison.OrdinalIgnoreCase);
    }

    private static string EnsureLeadingSlash(string path)
    {
        return path.StartsWith(PathSeparator) ? path : $"{PathSeparator}{path}";
    }

    private static string BuildTenantUrl(string subdomain, string domain, string path)
    {
        return $"{HttpsScheme}{subdomain}.{domain}{path}";
    }
}

using System.Text.RegularExpressions;
using StudioB2B.Domain.Constants;

namespace StudioB2B.Domain.Extensions;

public static partial class StringExtensions
{
    public static string NormalizeDomain(this string domain)
    {
        if (string.IsNullOrEmpty(domain))
            return string.Empty;

        var span = domain.AsSpan();

        if (span.StartsWith(WebConstants.HttpsScheme, StringComparison.OrdinalIgnoreCase))
            span = span[WebConstants.HttpsScheme.Length..];
        else if (span.StartsWith(WebConstants.HttpScheme, StringComparison.OrdinalIgnoreCase))
            span = span[WebConstants.HttpScheme.Length..];

        return span.TrimEnd('/').ToString();
    }

    public static string GetTenantUrl(this string subdomain, string masterDomain, string path = "/")
    {
        var normalizedDomain = NormalizeDomain(masterDomain);
        var sanitizedPath = string.IsNullOrEmpty(path) ||
                            path.StartsWith(WebConstants.HttpScheme, StringComparison.OrdinalIgnoreCase) ||
                            path.StartsWith(WebConstants.HttpsScheme, StringComparison.OrdinalIgnoreCase)
                            ? WebConstants.PathSeparator.ToString()
                            : path.StartsWith(WebConstants.PathSeparator) ? path : $"{WebConstants.PathSeparator}{path}";

        return $"{WebConstants.HttpsScheme}{subdomain}.{normalizedDomain}{sanitizedPath}";
    }

    public static bool IsValidSubdomain(this string subdomain)
    {
        if (string.IsNullOrWhiteSpace(subdomain)) return false;
        if (subdomain.Length is < 3 or > 30) return false;
        return SubdomainRegex().IsMatch(subdomain);
    }

    [GeneratedRegex(@"^[a-z0-9][a-z0-9\-]*[a-z0-9]$|^[a-z0-9]$")]
    private static partial Regex SubdomainRegex();
}

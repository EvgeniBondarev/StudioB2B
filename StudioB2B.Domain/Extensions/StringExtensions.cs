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

        if (span.StartsWith(Constants.Constants.HttpsScheme, StringComparison.OrdinalIgnoreCase))
            span = span[Constants.Constants.HttpsScheme.Length..];
        else if (span.StartsWith(Constants.Constants.HttpScheme, StringComparison.OrdinalIgnoreCase))
            span = span[Constants.Constants.HttpScheme.Length..];

        return span.TrimEnd('/').ToString();
    }

    public static string GetTenantUrl(this string subdomain, string masterDomain, string path = "/")
    {
        var normalizedDomain = NormalizeDomain(masterDomain);
        var sanitizedPath = string.IsNullOrEmpty(path) ||
                            path.StartsWith(Constants.Constants.HttpScheme, StringComparison.OrdinalIgnoreCase) ||
                            path.StartsWith(Constants.Constants.HttpsScheme, StringComparison.OrdinalIgnoreCase)
                            ? Constants.Constants.PathSeparator.ToString()
                            : path.StartsWith(Constants.Constants.PathSeparator) ? path : $"{Constants.Constants.PathSeparator}{path}";

        return $"{Constants.Constants.HttpsScheme}{subdomain}.{normalizedDomain}{sanitizedPath}";
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

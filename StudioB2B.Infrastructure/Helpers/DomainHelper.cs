namespace StudioB2B.Infrastructure.Helpers;

/// <summary>
/// Утилита нормализации доменных имён.
/// Убирает протокол и trailing slash: "https://example.com/" → "example.com"
/// </summary>
public static class DomainHelper
{
    public static string Normalize(string domain)
    {
        if (string.IsNullOrEmpty(domain))
            return string.Empty;

        var span = domain.AsSpan();

        if (span.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            span = span["https://".Length..];
        else if (span.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            span = span["http://".Length..];

        return span.TrimEnd('/').ToString();
    }
}

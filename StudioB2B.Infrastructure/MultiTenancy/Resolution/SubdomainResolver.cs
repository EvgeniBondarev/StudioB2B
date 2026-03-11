using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace StudioB2B.Infrastructure.MultiTenancy.Resolution;

public class SubdomainResolver : ISubdomainResolver
{
    private readonly MultiTenancyOptions _options;
    private readonly ILogger<SubdomainResolver> _logger;
    private readonly string _masterDomain;
    private readonly string _masterDomainSuffix; // ".studiob2b.ru"

    public SubdomainResolver(IOptions<MultiTenancyOptions> options, ILogger<SubdomainResolver> logger)
    {
        _options = options.Value;
        _logger = logger;
        _masterDomain = DomainHelper.Normalize(_options.MasterDomain);
        _masterDomainSuffix = $".{_masterDomain}";
    }

    public string? Resolve(HostString host)
    {
        var hostValue = host.Host;

        // 1. localhost: demo.localhost → demo
        if (hostValue.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
        {
            var sub = hostValue[..^".localhost".Length];
            return IsUsable(sub) ? sub.ToLowerInvariant() : null;
        }

        // 2. Точное совпадение с мастер-доменом → нет субдомена
        if (string.Equals(hostValue, _masterDomain, StringComparison.OrdinalIgnoreCase))
            return null;

        // 3. Субдомен мастер-домена: demo.studiob2b.ru → demo
        if (hostValue.EndsWith(_masterDomainSuffix, StringComparison.OrdinalIgnoreCase))
        {
            var prefix = hostValue[..^_masterDomainSuffix.Length];
            return IsUsable(prefix) ? prefix.ToLowerInvariant() : null;
        }

        // 4. Фоллбэк: хост содержит мастер-домен, но суффикс не совпал
        // (защита от неточной конфигурации MasterDomain)
        if (hostValue.Contains(_masterDomain, StringComparison.OrdinalIgnoreCase) &&
            !hostValue.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
        {
            var parts = hostValue.Split('.');
            if (parts.Length >= 3 && IsUsable(parts[0]))
            {
                _logger.LogWarning("Subdomain resolved via fallback for host: {Host}", hostValue);
                return parts[0].ToLowerInvariant();
            }
        }
        return null;
    }

    private bool IsUsable(string subdomain) =>
        !string.IsNullOrEmpty(subdomain) &&
        !_options.ReservedSubdomains.Contains(subdomain, StringComparer.OrdinalIgnoreCase);
}

using Microsoft.Extensions.Options;
using StudioB2B.Application.Common.Interfaces;
using StudioB2B.Domain.Extensions;
using StudioB2B.Domain.Options;

namespace StudioB2B.Infrastructure.MultiTenancy;

public class SubdomainResolver : ISubdomainResolver
{
    private readonly MultiTenancyOptions _options;
    private readonly string _masterDomain;
    private readonly string _masterDomainSuffix; // ".studiob2b.ru"

    public SubdomainResolver(IOptions<MultiTenancyOptions> options)
    {
        _options = options.Value;
        _masterDomain = _options.MasterDomain.NormalizeDomain();
        _masterDomainSuffix = $".{_masterDomain}";
    }

    public string? Resolve(string host)
    {
        // 1. localhost: demo.localhost → demo
        if (host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
        {
            var sub = host[..^".localhost".Length];
            return IsUsable(sub) ? sub.ToLowerInvariant() : null;
        }

        // 2. Точное совпадение с мастер-доменом → нет субдомена
        if (string.Equals(host, _masterDomain, StringComparison.OrdinalIgnoreCase))
            return null;

        // 3. Субдомен мастер-домена: demo.studiob2b.ru → demo
        if (host.EndsWith(_masterDomainSuffix, StringComparison.OrdinalIgnoreCase))
        {
            var prefix = host[..^_masterDomainSuffix.Length];
            return IsUsable(prefix) ? prefix.ToLowerInvariant() : null;
        }

        // 4. Фоллбэк: хост содержит мастер-домен, но суффикс не совпал
        //    (защита от неточной конфигурации MasterDomain)
        if (host.Contains(_masterDomain, StringComparison.OrdinalIgnoreCase) &&
            !host.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
        {
            var parts = host.Split('.');
            if (parts.Length >= 3 && IsUsable(parts[0]))
            {
                return parts[0].ToLowerInvariant();
            }
        }
        return null;
    }

    private bool IsUsable(string subdomain) =>
        !string.IsNullOrEmpty(subdomain) &&
        !_options.ReservedSubdomains.Contains(subdomain, StringComparer.OrdinalIgnoreCase);
}

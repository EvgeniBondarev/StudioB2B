using System.Collections.Concurrent;

namespace StudioB2B.Web.Services;

/// <summary>
/// Singleton-хранилище одноразовых токенов обмена.
/// Blazor-компонент сохраняет сюда JWT и получает короткоживущий exchange-ключ,
/// затем редиректит браузер на /auth/set-cookie?t={key} — там ключ обменивается
/// на JWT и устанавливается HttpOnly-кука.
/// </summary>
public sealed class TokenExchangeService
{
    private readonly ConcurrentDictionary<string, ExchangeEntry> _tokens = new();

    public string Store(string jwt)
    {
        var key = Guid.NewGuid().ToString("N");
        var entry = new ExchangeEntry(jwt, DateTimeOffset.UtcNow.AddSeconds(30));
        _tokens[key] = entry;
        return key;
    }

    public string? Consume(string key)
    {
        if (_tokens.TryRemove(key, out var entry))
        {
            if (entry.ExpiresAt > DateTimeOffset.UtcNow)
                return entry.Jwt;
        }
        return null;
    }

    private record ExchangeEntry(string Jwt, DateTimeOffset ExpiresAt);
}


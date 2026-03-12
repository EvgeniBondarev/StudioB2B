using System.Text.Json;
using System.Text.Json.Serialization;

namespace StudioB2B.Infrastructure.Helpers.Http;

public static class HttpResponseMessageExtensions
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false) }
    };

    /// <summary>
    /// Deserializes response body from stream without loading into a string (memory efficient)
    /// </summary>
    public static async Task<T?> ReadFromJsonAsync<T>(this HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        response.EnsureSuccessStatusCode();
        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<T>(stream, DefaultOptions, cancellationToken);
    }
}

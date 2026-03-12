using System.Text.Json;

namespace StudioB2B.Infrastructure.Helpers;

/// <summary>
/// Сериализует DateTime всегда как UTC (добавляет суффикс Z), чего требует Ozon API.
/// </summary>
public sealed class UtcDateTimeJsonConverter : System.Text.Json.Serialization.JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.GetDateTime();

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => writer.WriteStringValue(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}

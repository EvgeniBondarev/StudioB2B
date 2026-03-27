using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonReturnStatusRefDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("sys_name")]
    public string? SysName { get; set; }
}

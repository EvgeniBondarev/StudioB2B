using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonReturnAdditionalInfoDto
{
    [JsonPropertyName("is_opened")]
    public bool IsOpened { get; set; }

    [JsonPropertyName("is_super_econom")]
    public bool IsSuperEconom { get; set; }
}

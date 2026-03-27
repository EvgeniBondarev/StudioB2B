using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonReturnCompensationStatusDto
{
    [JsonPropertyName("status")]
    public OzonReturnStatusRefDto? Status { get; set; }

    [JsonPropertyName("change_moment")]
    public DateTime? ChangeMoment { get; set; }
}

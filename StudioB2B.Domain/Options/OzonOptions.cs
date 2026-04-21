namespace StudioB2B.Domain.Options;

public class OzonOptions
{
    public const string SectionName = "Ozon";

    public string BaseAddress { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; }
}


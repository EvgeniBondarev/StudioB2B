namespace StudioB2B.Shared;

public class NewClientModelDto
{
    public Guid? ClientTypeId { get; set; }

    public List<Guid> ModeIds { get; set; } = new();

    public string ApiId { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;
}

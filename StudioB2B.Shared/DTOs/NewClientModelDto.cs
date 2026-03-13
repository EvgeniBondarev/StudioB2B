namespace StudioB2B.Shared.DTOs;

public class NewClientModelDto
{
    public Guid? ClientTypeId { get; set; }
    public Guid? ModeId { get; set; }
    public string ApiId { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
}

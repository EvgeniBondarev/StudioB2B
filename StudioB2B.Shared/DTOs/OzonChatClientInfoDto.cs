namespace StudioB2B.Shared.DTOs;

public class OzonChatClientInfoDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ApiId { get; set; } = string.Empty;
    public string EncryptedApiKey { get; set; } = string.Empty;
}

namespace StudioB2B.Shared;

public class OzonChatPageDto
{
    public List<OzonChatViewModelDto> Chats { get; set; } = new();

    /// <summary>Курсор для следующей страницы. null — страниц больше нет.</summary>
    public string? NextCursor { get; set; }

    public bool HasMore => NextCursor is not null;
}

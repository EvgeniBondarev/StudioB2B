using System.Text.Json.Serialization;

namespace StudioB2B.Infrastructure.Integrations.Ozon.Models.Chat;

public class OzonChatListRequest
{
    [JsonPropertyName("filter")]
    public OzonChatListFilter? Filter { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 30;

    [JsonPropertyName("cursor")]
    public string? Cursor { get; set; }
}

public class OzonChatListFilter
{
    [JsonPropertyName("chat_status")]
    public string? ChatStatus { get; set; }

    [JsonPropertyName("unread_only")]
    public bool? UnreadOnly { get; set; }
}

public class OzonChatListResponse
{
    [JsonPropertyName("chats")]
    public List<OzonChatItem> Chats { get; set; } = new();

    [JsonPropertyName("total_unread_count")]
    public int TotalUnreadCount { get; set; }

    [JsonPropertyName("cursor")]
    public string? Cursor { get; set; }

    [JsonPropertyName("has_next")]
    public bool HasNext { get; set; }
}

public class OzonChatItem
{
    [JsonPropertyName("chat")]
    public OzonChatDto? Chat { get; set; }

    [JsonPropertyName("first_unread_message_id")]
    public ulong? FirstUnreadMessageId { get; set; }

    [JsonPropertyName("last_message_id")]
    public ulong? LastMessageId { get; set; }

    [JsonPropertyName("unread_count")]
    public int UnreadCount { get; set; }
}

public class OzonChatDto
{
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("chat_id")]
    public string ChatId { get; set; } = string.Empty;

    [JsonPropertyName("chat_status")]
    public string ChatStatus { get; set; } = string.Empty;

    [JsonPropertyName("chat_type")]
    public string ChatType { get; set; } = string.Empty;
}

public class OzonChatHistoryRequest
{
    [JsonPropertyName("chat_id")]
    public string ChatId { get; set; } = string.Empty;

    [JsonPropertyName("direction")]
    public string Direction { get; set; } = "Backward";

    [JsonPropertyName("from_message_id")]
    public ulong? FromMessageId { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 50;
}

public class OzonChatHistoryResponse
{
    [JsonPropertyName("has_next")]
    public bool HasNext { get; set; }

    [JsonPropertyName("messages")]
    public List<OzonChatMessage> Messages { get; set; } = new();
}

public class OzonChatMessage
{
    [JsonPropertyName("message_id")]
    public ulong MessageId { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("data")]
    public List<string> Data { get; set; } = new();

    [JsonPropertyName("is_image")]
    public bool IsImage { get; set; }

    [JsonPropertyName("is_read")]
    public bool IsRead { get; set; }

    [JsonPropertyName("user")]
    public OzonChatMessageUser? User { get; set; }

    [JsonPropertyName("context")]
    public OzonChatMessageContext? Context { get; set; }

    [JsonPropertyName("moderate_image_status")]
    public string? ModerateImageStatus { get; set; }
}

public class OzonChatMessageUser
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}

public class OzonChatMessageContext
{
    [JsonPropertyName("order_number")]
    public string? OrderNumber { get; set; }

    [JsonPropertyName("sku")]
    public string? Sku { get; set; }
}

public class OzonSendMessageRequest
{
    [JsonPropertyName("chat_id")]
    public string ChatId { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

public class OzonSendMessageResponse
{
    [JsonPropertyName("result")]
    public string Result { get; set; } = string.Empty;
}

public class OzonSendFileRequest
{
    [JsonPropertyName("base64_content")]
    public string Base64Content { get; set; } = string.Empty;

    [JsonPropertyName("chat_id")]
    public string ChatId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class OzonSendFileResponse
{
    [JsonPropertyName("result")]
    public string Result { get; set; } = string.Empty;
}

public class OzonReadChatRequest
{
    [JsonPropertyName("chat_id")]
    public string ChatId { get; set; } = string.Empty;

    [JsonPropertyName("from_message_id")]
    public ulong? FromMessageId { get; set; }
}

public class OzonReadChatResponse
{
    [JsonPropertyName("unread_count")]
    public int UnreadCount { get; set; }
}


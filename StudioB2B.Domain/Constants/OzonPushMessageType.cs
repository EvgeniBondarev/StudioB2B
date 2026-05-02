namespace StudioB2B.Domain.Constants;

/// <summary>
/// Константы типов push-уведомлений от Ozon (поле message_type в payload).
/// </summary>
public static class OzonPushMessageType
{
    public const string Ping = "TYPE_PING";

    public const string NewPosting = "TYPE_NEW_POSTING";

    public const string PostingCancelled = "TYPE_POSTING_CANCELLED";

    public const string StateChanged = "TYPE_STATE_CHANGED";

    public const string CutoffDateChanged = "TYPE_CUTOFF_DATE_CHANGED";

    public const string DeliveryDateChanged = "TYPE_DELIVERY_DATE_CHANGED";

    public const string CreateOrUpdateItem = "TYPE_CREATE_OR_UPDATE_ITEM";

    public const string CreateItem = "TYPE_CREATE_ITEM";

    public const string UpdateItem = "TYPE_UPDATE_ITEM";

    public const string StocksChanged = "TYPE_STOCKS_CHANGED";

    public const string NewMessage = "TYPE_NEW_MESSAGE";

    public const string UpdateMessage = "TYPE_UPDATE_MESSAGE";

    public const string MessageRead = "TYPE_MESSAGE_READ";

    public const string ChatClosed = "TYPE_CHAT_CLOSED";

    /// <summary>Типы, связанные с чатами — при получении открывают доску задач.</summary>
    public static readonly HashSet<string> ChatTypes =
    [
        NewMessage,
        MessageRead,
        ChatClosed
    ];
}


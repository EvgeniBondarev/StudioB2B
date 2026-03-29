namespace StudioB2B.Shared;

/// <summary>
/// Предпросмотр проведения документа: правила и значения для ввода.
/// </summary>
public class TransactionApplyPreviewDto
{
    public string TransactionName { get; set; } = string.Empty;

    public string ToStatusName { get; set; } = string.Empty;

    public List<TransactionApplyRulePreviewDto> Rules { get; set; } = [];

    public List<TransactionApplyFieldRulePreviewDto> FieldRules { get; set; } = [];
}


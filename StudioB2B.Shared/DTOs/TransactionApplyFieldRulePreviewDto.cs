using StudioB2B.Domain.Constants;

namespace StudioB2B.Shared;

/// <summary>
/// Предпросмотр правила изменения поля.
/// </summary>
public class TransactionApplyFieldRulePreviewDto
{
    public Guid RuleId { get; set; }

    public string EntityPath { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public TransactionFieldValueSourceEnum ValueSource { get; set; }

    public string? FixedValue { get; set; }

    public TransactionFieldValueTypeEnum ValueType { get; set; }

    public FieldReferenceTypeEnum ReferenceType { get; set; }

    public bool IsRequired { get; set; }
    public bool RequiresUserInput => ValueSource == TransactionFieldValueSourceEnum.UserInput;
}


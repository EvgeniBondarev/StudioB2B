using StudioB2B.Domain.Entities.Orders;

namespace StudioB2B.Application.Common;

/// <summary>
/// Предпросмотр проведения документа: правила и значения для ввода.
/// </summary>
public class TransactionApplyPreview
{
    public string TransactionName { get; set; } = string.Empty;
    public string ToStatusName { get; set; } = string.Empty;
    public List<TransactionApplyRulePreview> Rules { get; set; } = [];
    public List<TransactionApplyFieldRulePreview> FieldRules { get; set; } = [];
}

/// <summary>
/// Предпросмотр правила изменения поля.
/// </summary>
public class TransactionApplyFieldRulePreview
{
    public Guid RuleId { get; set; }
    public string EntityPath { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public TransactionFieldValueSource ValueSource { get; set; }
    public string? FixedValue { get; set; }
    public TransactionFieldValueType ValueType { get; set; }
    public FieldReferenceType ReferenceType { get; set; }
    public bool IsRequired { get; set; }
    public bool RequiresUserInput => ValueSource == TransactionFieldValueSource.UserInput;
}

/// <summary>
/// Предпросмотр одного правила: тип цены, источник, вычисленное значение или необходимость ввода.
/// </summary>
public class TransactionApplyRulePreview
{
    public Guid RuleId { get; set; }
    public Guid PriceTypeId { get; set; }
    public string PriceTypeName { get; set; } = string.Empty;
    public Guid? ProductId { get; set; }
    public string? ProductName { get; set; }
    public TransactionValueSource ValueSource { get; set; }
    /// <summary>Формула (для Formula).</summary>
    public string? Formula { get; set; }
    /// <summary>Вычисленное значение (для Formula).</summary>
    public decimal? ComputedValue { get; set; }
    /// <summary>Расшифровка формулы: подстановка переменных и результат (напр. "100 * 0.85 = 85").</summary>
    public string? FormulaBreakdown { get; set; }
    /// <summary>Требуется ввод пользователя при проведении.</summary>
    public bool RequiresUserInput => ValueSource == TransactionValueSource.UserInput;
    /// <summary>Обязательное поле при проведении.</summary>
    public bool IsRequired { get; set; }
}

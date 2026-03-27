using StudioB2B.Domain.Constants;

namespace StudioB2B.Shared;

/// <summary>
/// Предпросмотр одного правила: тип цены, источник, вычисленное значение или необходимость ввода.
/// </summary>
public class TransactionApplyRulePreviewDto
{
    public Guid RuleId { get; set; }

    public Guid PriceTypeId { get; set; }

    public string PriceTypeName { get; set; } = string.Empty;

    public Guid? ProductId { get; set; }

    public string? ProductName { get; set; }

    public TransactionValueSourceEnum ValueSource { get; set; }

    /// <summary>Формула (для Formula).</summary>
    public string? Formula { get; set; }

    /// <summary>Вычисленное значение (для Formula).</summary>
    public decimal? ComputedValue { get; set; }

    /// <summary>Расшифровка формулы: подстановка переменных и результат (напр. "100 * 0.85 = 85").</summary>
    public string? FormulaBreakdown { get; set; }

    /// <summary>Требуется ввод пользователя при проведении.</summary>
    public bool RequiresUserInput => ValueSource == TransactionValueSourceEnum.UserInput;
    /// <summary>Обязательное поле при проведении.</summary>
    public bool IsRequired { get; set; }
}


namespace StudioB2B.Domain.Constants;

/// <summary>
/// Источник значения для правила транзакции: формула или ввод при проведении.
/// </summary>
public enum TransactionValueSourceEnum
{
    /// <summary>Вычисляется по формуле (может использовать другие типы цен).</summary>
    Formula = 0,

    /// <summary>Значение вводится пользователем при проведении транзакции.</summary>
    UserInput = 1
}

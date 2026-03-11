namespace StudioB2B.Domain.Constants;

/// <summary>
/// Источник значения для правила изменения поля в транзакции.
/// </summary>
public enum TransactionFieldValueSourceEnum
{
    /// <summary>Фиксированное значение, задаётся при настройке транзакции.</summary>
    Fixed = 0,

    /// <summary>Значение вводится пользователем при проведении транзакции.</summary>
    UserInput = 1
}

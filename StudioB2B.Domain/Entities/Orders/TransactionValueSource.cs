namespace StudioB2B.Domain.Entities.Orders;

/// <summary>
/// Источник значения для правила документа: формула или ввод при проведении.
/// </summary>
public enum TransactionValueSource
{
    /// <summary>Вычисляется по формуле (может использовать другие типы цен).</summary>
    Formula = 0,

    /// <summary>Значение вводится пользователем при проведении документа.</summary>
    UserInput = 1
}

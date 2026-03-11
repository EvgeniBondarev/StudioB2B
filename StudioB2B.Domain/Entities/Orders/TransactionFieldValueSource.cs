namespace StudioB2B.Domain.Entities.Orders;

/// <summary>
/// Источник значения для правила изменения поля в документе.
/// </summary>
public enum TransactionFieldValueSource
{
    /// <summary>Фиксированное значение, задаётся при настройке документа.</summary>
    Fixed = 0,

    /// <summary>Значение вводится пользователем при проведении документа.</summary>
    UserInput = 1
}

namespace StudioB2B.Domain.Entities.Common;

/// <summary>
/// Помечает свойство сущности как исключённое из журнала изменений (<see cref="FieldAuditLog"/>).
/// Поля с этим атрибутом не сохраняются в историю независимо от типа операции.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class SkipAuditAttribute : Attribute;


namespace StudioB2B.Web.Components.Common;

/// <summary>
/// Описывает одну сущность для отображения в диалоге истории изменений.
/// </summary>
public sealed record AuditSubject(string EntityName, string EntityId, string Label);

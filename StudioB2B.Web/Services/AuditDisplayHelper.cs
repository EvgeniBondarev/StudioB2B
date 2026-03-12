using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace StudioB2B.Web.Services;

/// <summary>
/// Вспомогательный класс для получения отображаемых названий сущностей и полей
/// из атрибутов <see cref="DisplayAttribute"/> через рефлексию.
/// Результаты кэшируются в статических словарях.
/// </summary>
public static class AuditDisplayHelper
{
    // Сборки, в которых хранятся доменные типы
    private static readonly Assembly[] _scannedAssemblies =
    [
        Assembly.Load("StudioB2B.Domain"),
        Assembly.Load("StudioB2B.Infrastructure")
    ];

    /// <summary>CLR short name → Display Name класса</summary>
    private static readonly ConcurrentDictionary<string, string> _entityNames = new(StringComparer.Ordinal);

    /// <summary>"EntityName|FieldName" → Display Name свойства</summary>
    private static readonly ConcurrentDictionary<string, string> _fieldNames = new(StringComparer.Ordinal);

    private static bool _initialized;
    private static readonly object _lock = new();

    // Public API

    /// <summary>Возвращает отображаемое название сущности по её CLR-имени (без пространства имён).</summary>
    public static string GetEntityDisplayName(string entityName)
    {
        EnsureInitialized();
        return _entityNames.TryGetValue(entityName, out var display) ? display : entityName;
    }

    /// <summary>Возвращает отображаемое название поля для заданной сущности.</summary>
    public static string GetFieldDisplayName(string entityName, string fieldName)
    {
        EnsureInitialized();
        var key = $"{entityName}|{fieldName}";
        return _fieldNames.TryGetValue(key, out var display) ? display : fieldName;
    }

    // Initialization

    private static void EnsureInitialized()
    {
        if (_initialized) return;

        lock (_lock)
        {
            if (_initialized) return;
            BuildCache();
            _initialized = true;
        }
    }

    private static void BuildCache()
    {
        foreach (var assembly in _scannedAssemblies)
        {
            IEnumerable<Type> types;
            try { types = assembly.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { types = ex.Types.OfType<Type>(); }

            foreach (var type in types)
            {
                if (!type.IsClass || type.IsAbstract || type.IsGenericType)
                    continue;

                var shortName = type.Name;

                // Имя класса из [Display(Name = "...")]
                var classDisplay = type.GetCustomAttribute<DisplayAttribute>();
                if (classDisplay?.Name != null)
                    _entityNames[shortName] = classDisplay.Name;

                // Имена свойств из [Display(Name = "...")]
                foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    var propDisplay = prop.GetCustomAttribute<DisplayAttribute>();
                    if (propDisplay?.Name != null)
                        _fieldNames[$"{shortName}|{prop.Name}"] = propDisplay.Name;
                }
            }
        }
    }
}


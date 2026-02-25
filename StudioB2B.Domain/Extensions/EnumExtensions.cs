using System.ComponentModel;
using System.Reflection;
using StudioB2B.Domain.Entities.Common;

namespace StudioB2B.Domain.Extensions;

public static class EnumExtensions
{
    public static List<T> ToList<T>(this Type enumType)
        where T: IHasId, IHasName, new()
    {
        return Enum.GetValues(enumType)
            .Cast<Enum>()
            .Select(e => new T
                         {
                             Id = Guid.NewGuid(),
                             Name = e.GetDescription()
                         })
            .ToList();
    }

    private static string GetDescription(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
        return attribute?.Description ?? value.ToString();
    }
}

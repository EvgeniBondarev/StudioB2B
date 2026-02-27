using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace StudioB2B.Web.Helpers;

public static class DisplayNameHelper
{
    public static string For<TModel>(string propertyName)
    {
        var prop = typeof(TModel).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (prop == null)
        {
            return propertyName;
        }

        var attr = prop.GetCustomAttribute<DisplayAttribute>();
        return attr?.Name ?? prop.Name;
    }
}


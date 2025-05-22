namespace MapHive;

using System.Data;
using Microsoft.AspNetCore.Mvc.Rendering;

public static class ViewContextExtensions
{
    public static T GetRouteValueOrThrow<T>(this ViewContext viewContext, string key)
    {
        object? value = viewContext.RouteData.Values.ContainsKey(key) ? viewContext.RouteData.Values[key] : null;
        return value == null
            ? throw new NoNullAllowedException($"Route value '{key}' is not present.")
            : (T)value;
    }

    public static string IdOrThrow(this ViewContext viewContext)
    {
        return GetRouteValueOrThrow<string>(viewContext, "id");
    }
}

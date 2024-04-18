using System.Reflection;

namespace Iceshrimp.Backend.Core.Extensions;

public static class AttributeExtensions
{
	public static T? GetCustomAttribute<T>(this Type type, bool inherit = false) where T : Attribute
	{
		return type.GetCustomAttribute(typeof(T), inherit) as T;
	}
}
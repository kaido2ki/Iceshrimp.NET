using System.Reflection;

namespace Iceshrimp.Backend.Core.Helpers;

public static class AssemblyHelpers
{
	public static IEnumerable<Type> GetTypesWithAttribute(Type attribute, Assembly? assembly = null)
	{
		assembly ??= Assembly.GetExecutingAssembly();
		return assembly.GetTypes().Where(type => Attribute.IsDefined(type, attribute));
	}

	public static IEnumerable<Type> GetImplementationsOfInterface(Type @interface, Assembly? assembly = null)
	{
		assembly ??= Assembly.GetExecutingAssembly();
		return assembly.GetTypes()
		               .Where(type => type is { IsAbstract: false, IsClass: true } &&
		                              type.GetInterfaces().Contains(@interface));
	}
}
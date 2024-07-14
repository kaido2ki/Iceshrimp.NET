using System.Reflection;

namespace Iceshrimp.Backend.Core.Helpers;

public static class AssemblyHelpers
{
	public static string GetEmbeddedResource(string resourceName)
	{
		var       stream = GetEmbeddedResourceStream(resourceName);
		using var reader = new StreamReader(stream);
		return reader.ReadToEnd();
	}

	public static Stream GetEmbeddedResourceStream(string resourceName)
	{
		var assembly     = Assembly.GetExecutingAssembly();
		var assemblyName = assembly.GetName().Name;
		resourceName = $"{assemblyName}.{resourceName}";
		return assembly.GetManifestResourceStream(resourceName) ??
		       throw new Exception($"Failed to get embedded resource {resourceName} from assembly.");
	}
}
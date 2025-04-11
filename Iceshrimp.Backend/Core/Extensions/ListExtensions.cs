namespace Iceshrimp.Backend.Core.Extensions;

public static class ListExtensions
{
	public static void AddRangeIfMissing<T>(this List<T> list, params ReadOnlySpan<T> source)
	{
		foreach (var item in source)
			if (!list.Contains(item))
				list.Add(item);
	}
}

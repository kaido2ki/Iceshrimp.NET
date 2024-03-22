namespace Iceshrimp.Backend.Core.Extensions;

public static class ListDestructuringExtensions
{
	public static void Deconstruct<T>(this IList<T> list, out T item1)
	{
		if (list.Count != 1)
			throw new Exception("This deconstructor only takes lists of length 1");

		item1 = list[0];
	}

	public static void Deconstruct<T>(this IList<T> list, out T item1, out T item2)
	{
		if (list.Count != 2)
			throw new Exception("This deconstructor only takes lists of length 2");

		item1 = list[0];
		item2 = list[1];
	}

	public static void Deconstruct<T>(this IList<T> list, out T item1, out T item2, out T item3)
	{
		if (list.Count != 3)
			throw new Exception("This deconstructor only takes lists of length 3");

		item1 = list[0];
		item2 = list[1];
		item3 = list[2];
	}

	public static void Deconstruct<T>(this IList<T> list, out T item1, out T item2, out T item3, out T item4)
	{
		if (list.Count != 4)
			throw new Exception("This deconstructor only takes lists of length 4");

		item1 = list[0];
		item2 = list[1];
		item3 = list[2];
		item4 = list[3];
	}
	
	public static void Deconstruct<T>(this IList<T> list, out T item1, out T item2, out T item3, out T item4, out T item5)
	{
		if (list.Count != 5)
			throw new Exception("This deconstructor only takes lists of length 5");

		item1 = list[0];
		item2 = list[1];
		item3 = list[2];
		item4 = list[3];
		item5 = list[4];
	}
}
namespace Iceshrimp.Backend.Core.Extensions;

public static class ArrayDestructuringExtensions
{
	public static void Deconstruct<T>(this T[] array, out T item1)
	{
		if (array.Length != 1)
			throw new Exception("This deconstructor only takes arrays of length 1");

		item1 = array[0];
	}

	public static void Deconstruct<T>(this T[] array, out T item1, out T item2)
	{
		if (array.Length != 2)
			throw new Exception("This deconstructor only takes arrays of length 2");

		item1 = array[0];
		item2 = array[1];
	}

	public static void Deconstruct<T>(this T[] array, out T item1, out T item2, out T item3)
	{
		if (array.Length != 3)
			throw new Exception("This deconstructor only takes arrays of length 3");

		item1 = array[0];
		item2 = array[1];
		item3 = array[2];
	}

	public static void Deconstruct<T>(this T[] array, out T item1, out T item2, out T item3, out T item4)
	{
		if (array.Length != 4)
			throw new Exception("This deconstructor only takes arrays of length 4");

		item1 = array[0];
		item2 = array[1];
		item3 = array[2];
		item4 = array[3];
	}

	public static void Deconstruct<T>(this T[] array, out T item1, out T item2, out T item3, out T item4, out T item5)
	{
		if (array.Length != 5)
			throw new Exception("This deconstructor only takes arrays of length 5");

		item1 = array[0];
		item2 = array[1];
		item3 = array[2];
		item4 = array[3];
		item5 = array[4];
	}
}
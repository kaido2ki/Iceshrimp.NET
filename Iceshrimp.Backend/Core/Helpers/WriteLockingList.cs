using System.Collections;

namespace Iceshrimp.Backend.Core.Helpers;

public class WriteLockingList<T> : ICollection<T>
{
	private readonly List<T> _list = [];

	public IEnumerator<T>   GetEnumerator() => _list.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();

	public void Add(T item)
	{
		lock (_list) _list.Add(item);
	}
	
	public void AddRange(IEnumerable<T> item)
	{
		lock (_list) _list.AddRange(item);
	}

	public void Clear()
	{
		lock (_list) _list.Clear();
	}

	public bool Contains(T item) => _list.Contains(item);

	public void CopyTo(T[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

	public bool Remove(T item)
	{
		lock (_list) return _list.Remove(item);
	}

	public int  Count      => _list.Count;
	public bool IsReadOnly => ((ICollection<T>)_list).IsReadOnly;
}
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Iceshrimp.Backend.Core.Helpers;

[SuppressMessage("ReSharper", "InconsistentlySynchronizedField",
                 Justification = "This is intentional (it's a *write* locking list, after all)")]
public class WriteLockingList<T>(IEnumerable<T>? sourceCollection = null) : ICollection<T>
{
	private readonly List<T> _list = sourceCollection?.ToList() ?? [];

	public IEnumerator<T>   GetEnumerator() => _list.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();

	public void Add(T item)
	{
		lock (_list) _list.Add(item);
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

	public bool AddIfMissing(T item)
	{
		lock (_list)
		{
			if (_list.Contains(item)) return false;
			_list.Add(item);
			return true;
		}
	}

	public void AddRange(IEnumerable<T> item)
	{
		lock (_list) _list.AddRange(item);
	}

	public int RemoveAll(Predicate<T> predicate)
	{
		lock (_list) return _list.RemoveAll(predicate);
	}
}
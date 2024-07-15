using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Iceshrimp.Backend.Core.Helpers;

[SuppressMessage("ReSharper", "InconsistentlySynchronizedField",
                 Justification = "This is intentional (it's a *write* locking list, after all)")]
public class WriteLockingList<T>(IEnumerable<T>? sourceCollection = null) : ICollection<T>
{
	private readonly List<T> _list = sourceCollection?.ToList() ?? [];
	private readonly Lock    _lock = new();

	public IEnumerator<T>   GetEnumerator() => _list.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();

	public void Add(T item)
	{
		lock (_lock) _list.Add(item);
	}

	public void Clear()
	{
		lock (_lock) _list.Clear();
	}

	public bool Contains(T item) => _list.Contains(item);

	public void CopyTo(T[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

	public bool Remove(T item)
	{
		lock (_lock) return _list.Remove(item);
	}

	public int  Count      => _list.Count;
	public bool IsReadOnly => ((ICollection<T>)_list).IsReadOnly;

	public bool AddIfMissing(T item)
	{
		lock (_lock)
		{
			if (_list.Contains(item)) return false;
			_list.Add(item);
			return true;
		}
	}

	public void AddRange(IEnumerable<T> item)
	{
		lock (_lock) _list.AddRange(item);
	}

	public int RemoveAll(Predicate<T> predicate)
	{
		lock (_lock) return _list.RemoveAll(predicate);
	}
}
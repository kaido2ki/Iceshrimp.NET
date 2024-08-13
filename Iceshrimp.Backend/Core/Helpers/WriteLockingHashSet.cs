using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Iceshrimp.Backend.Core.Helpers;

[SuppressMessage("ReSharper", "InconsistentlySynchronizedField",
                 Justification = "This is intentional (it's a *write* locking hash set, after all)")]
public class WriteLockingHashSet<T>(IEnumerable<T>? sourceCollection = null) : ICollection<T>
{
	private readonly HashSet<T> _set = sourceCollection?.ToHashSet() ?? [];

	public IEnumerator<T>   GetEnumerator() => _set.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => _set.GetEnumerator();

	public void Add(T item)
	{
		lock (_set) _set.Add(item);
	}

	public void Clear()
	{
		lock (_set) _set.Clear();
	}

	public bool Contains(T item) => _set.Contains(item);

	public void CopyTo(T[] array, int arrayIndex) => _set.CopyTo(array, arrayIndex);

	public bool Remove(T item)
	{
		lock (_set) return _set.Remove(item);
	}

	public int  Count      => _set.Count;
	public bool IsReadOnly => ((ICollection<T>)_set).IsReadOnly;

	public bool AddIfMissing(T item)
	{
		lock (_set) return _set.Add(item);
	}

	public void AddRange(IEnumerable<T> items)
	{
		lock (_set)
			foreach (var item in items)
				_set.Add(item);
	}

	public int RemoveWhere(Predicate<T> predicate)
	{
		lock (_set) return _set.RemoveWhere(predicate);
	}
}
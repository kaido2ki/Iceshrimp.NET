namespace Iceshrimp.Backend.Core.Helpers;

public class SemaphorePlus(int maxCount) : SemaphoreSlim(maxCount, maxCount)
{
	private readonly int _maxCount = maxCount;
	public           int ActiveCount => _maxCount - CurrentCount;
}
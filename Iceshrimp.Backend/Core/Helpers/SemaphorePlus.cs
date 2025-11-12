namespace Iceshrimp.Backend.Core.Helpers;

public class SemaphorePlus(int maxCount) : SemaphoreSlim(maxCount, maxCount)
{
	public int ActiveCount
	{
		get => field - CurrentCount;
	} = maxCount;

	public async Task WaitAndReleaseAsync(CancellationToken token)
	{
		await WaitAsync(token);
		Release();
	}
}
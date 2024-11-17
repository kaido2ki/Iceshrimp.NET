using Iceshrimp.Backend.Core.Extensions;

namespace Iceshrimp.Backend.Core.Services;

public class FollowupTaskService(
	IServiceScopeFactory serviceScopeFactory,
	ILogger<FollowupTaskService> logger
) : IScopedService
{
	public bool IsBackgroundWorker { get; private set; }

	public Task ExecuteTask(string taskName, Func<IServiceProvider, Task> work)
	{
		return Task.Run(async () =>
		{
			await using var scope = serviceScopeFactory.CreateAsyncScope();

			try
			{
				var instance = scope.ServiceProvider.GetRequiredService<FollowupTaskService>();
				instance.IsBackgroundWorker = true;
				await work(scope.ServiceProvider);
			}
			catch (Exception e)
			{
				logger.LogError("Failed to execute background task {name}: {error}", taskName, e.ToString());
			}
		});
	}
}
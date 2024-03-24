namespace Iceshrimp.Backend.Core.Services;

public class FollowupTaskService(IServiceScopeFactory serviceScopeFactory)
{
	public bool IsBackgroundWorker { get; private set; }

	public Task ExecuteTask(string taskName, Func<IServiceProvider, Task> work)
	{
		return Task.Run(async () =>
		{
			await using var scope = serviceScopeFactory.CreateAsyncScope();
			try
			{
				var provider = scope.ServiceProvider;
				var instance = provider.GetRequiredService<FollowupTaskService>();
				instance.IsBackgroundWorker = true;
				await work(provider);
			}
			catch (Exception e)
			{
				var logger = scope.ServiceProvider.GetRequiredService<ILogger<FollowupTaskService>>();
				logger.LogError("Failed to execute background task {name}: {error}", taskName, e.ToString());
			}
		});
	}
}
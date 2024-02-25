namespace Iceshrimp.Backend.Core.Services;

public class FollowupTaskService(IServiceScopeFactory serviceScopeFactory)
{
	public bool IsBackgroundWorker { get; private set; }

	public IServiceProvider? ServiceProvider { get; set; }

	public Task ExecuteTask(string taskName, Func<IServiceProvider, Task> work)
	{
		return Task.Run(async () =>
		{
			using var scope = serviceScopeFactory.CreateScope();
			try
			{
				var provider = scope.ServiceProvider;
				var instance = provider.GetRequiredService<FollowupTaskService>();
				instance.IsBackgroundWorker = true;
				instance.ServiceProvider    = ServiceProvider ?? provider;
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
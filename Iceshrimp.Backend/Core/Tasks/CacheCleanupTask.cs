using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Tasks;

[SuppressMessage("ReSharper", "UnusedType.Global", Justification = "Instantiated at runtime by CronService")]
public class CacheCleanupTask : ICronTask
{
	public async Task InvokeAsync(IServiceProvider provider)
	{
		var db = provider.GetRequiredService<DatabaseContext>();
		await db.CacheStore.Where(p => p.Expiry != null && p.Expiry < DateTime.UtcNow).ExecuteDeleteAsync();
	}

	public CronTaskType Type    => CronTaskType.Interval;
	public TimeSpan     Trigger => TimeSpan.FromMinutes(15);
}
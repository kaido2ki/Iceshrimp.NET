using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Tasks;

[SuppressMessage("ReSharper", "UnusedType.Global", Justification = "Instantiated at runtime by CronService")]
public class FilterExpiryTask : ICronTask
{
	public async Task InvokeAsync(IServiceProvider provider)
	{
		var db = provider.GetRequiredService<DatabaseContext>();
		await db.Filters.Where(p => p.Expiry != null && p.Expiry < DateTime.UtcNow - TimeSpan.FromMinutes(5))
		        .ExecuteDeleteAsync();
	}

	// Midnight
	public TimeSpan     Trigger => TimeSpan.Zero;
	public CronTaskType Type    => CronTaskType.Daily;
}
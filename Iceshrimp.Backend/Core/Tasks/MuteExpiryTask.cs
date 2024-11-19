using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Tasks;

[SuppressMessage("ReSharper", "UnusedType.Global", Justification = "Instantiated at runtime by CronService")]
public class MuteExpiryTask : ICronTask
{
	public async Task InvokeAsync(IServiceProvider provider)
	{
		var db = provider.GetRequiredService<DatabaseContext>();
		await db.Mutings.Where(p => p.ExpiresAt != null && p.ExpiresAt < DateTime.UtcNow - TimeSpan.FromMinutes(5))
		        .ExecuteDeleteAsync();
	}

	// Midnight
	public TimeSpan     Trigger => TimeSpan.Zero;
	public CronTaskType Type    => CronTaskType.Daily;
}
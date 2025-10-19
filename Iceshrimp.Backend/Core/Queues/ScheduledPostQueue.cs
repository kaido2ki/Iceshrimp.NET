using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Services;
using Microsoft.EntityFrameworkCore;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JR = System.Text.Json.Serialization.JsonRequiredAttribute;

namespace Iceshrimp.Backend.Core.Queues;

public class ScheduledPostQueue(int parallelism)
	: PostgresJobQueue<ScheduledPostJobData>("scheduled-post", ScheduledPostQueueProcessorDelegateAsync, parallelism, TimeSpan.FromSeconds(60))
{
	private static async Task ScheduledPostQueueProcessorDelegateAsync(
		Job job,
		ScheduledPostJobData jobData,
		IServiceProvider scope,
		CancellationToken token
	)
	{
		var db      = scope.GetRequiredService<DatabaseContext>();
		var noteSvc = scope.GetRequiredService<NoteService>();
		var logger  = scope.GetRequiredService<ILogger<BackgroundTaskQueue>>();
        
		var note = await db.Notes.IncludeUnpublished().IncludeCommonProperties().Where(p => p.Id == jobData.NoteId).FirstOrDefaultAsync(token);
		if (note == null)
		{
			logger.LogDebug("Failed to post scheduled note {id}: note not found in database", jobData.NoteId);
			return;
		}

		note.ScheduledAt = null;
		await noteSvc.PublishNoteAsync(note);
	}
}

public class ScheduledPostJobData
{
	[JR] [J("noteId")] public required string? NoteId { get; set; }
}
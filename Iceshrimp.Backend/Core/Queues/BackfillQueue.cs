using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Services;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JR = System.Text.Json.Serialization.JsonRequiredAttribute;

namespace Iceshrimp.Backend.Core.Queues;

public class BackfillQueue(int parallelism)
	: PostgresJobQueue<BackfillJobData>("backfill", BackfillQueueProcessorDelegateAsync,
	                                    parallelism, TimeSpan.FromMinutes(5))
{
	private static async Task BackfillQueueProcessorDelegateAsync(
		Job job,
		BackfillJobData jobData,
		IServiceProvider scope,
		CancellationToken token
	)
	{
		var logger = scope.GetRequiredService<ILogger<BackfillQueue>>();
		logger.LogDebug("Backfilling replies for note {id} as user {userId}", jobData.NoteId,
		                jobData.AuthenticatedUserId);

		var db = scope.GetRequiredService<DatabaseContext>();

		var note = await db.Notes.Where(n => n.Id == jobData.NoteId).FirstOrDefaultAsync(token);
		if (note == null)
			return;

		var user = jobData.AuthenticatedUserId == null
			? null
			: await db.Users.Where(u => u.Id == jobData.AuthenticatedUserId).FirstOrDefaultAsync(token);

		var noteSvc = scope.GetRequiredService<NoteService>();

		ASCollection? collection = null;
		if (jobData.Collection != null)
			collection = ASObject.Deserialize(JToken.Parse(jobData.Collection)) as ASCollection;

		await noteSvc.BackfillRepliesAsync(note, user, collection, jobData.RecursionLimit);
	}
}

public class BackfillJobData
{
	[JR] [J("noteId")]              public required string  NoteId              { get; set; }
	[JR] [J("recursionLimit")]      public required int     RecursionLimit      { get; set; }
	[JR] [J("authenticatedUserId")] public required string? AuthenticatedUserId { get; set; }
	[JR] [J("collection")]          public          string? Collection          { get; set; }
}
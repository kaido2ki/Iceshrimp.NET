using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Mastodon.Renderers;

public class PollRenderer(DatabaseContext db) : IScopedService
{
	public async Task<PollEntity> RenderAsync(Poll poll, User? user, PollRendererDto? data = null)
	{
		var voted = (data?.Voted ?? await GetVotedAsync([poll], user)).Contains(poll.NoteId);

		var ownVotes = (data?.OwnVotes ?? await GetOwnVotesAsync([poll], user)).Where(p => p.Key == poll.NoteId)
		                                                                  .Select(p => p.Value)
		                                                                  .DefaultIfEmpty([])
		                                                                  .First();

		var res = new PollEntity
		{
			Id          = poll.NoteId,
			Expired     = poll.ExpiresAt < DateTime.UtcNow,
			Multiple    = poll.Multiple,
			ExpiresAt   = poll.ExpiresAt?.ToStringIso8601Like(),
			VotesCount  = poll.Votes.Sum(),
			VotersCount = poll.VotersCount ?? poll.Votes.Sum(),
			Voted       = voted,
			OwnVotes    = ownVotes,
			Options = poll.Choices
			              .Select(p => new PollOptionEntity
			              {
				              Title = p, VotesCount = poll.Votes[poll.Choices.IndexOf(p)]
			              })
			              .ToList()
		};

		return res;
	}

	private async Task<List<string>> GetVotedAsync(IEnumerable<Poll> polls, User? user)
	{
		if (user == null) return [];
		return await db.PollVotes.Where(p => polls.Select(i => i.NoteId).Any(i => i == p.NoteId) && p.User == user)
		               .Select(p => p.NoteId)
		               .Distinct()
		               .ToListAsync();
	}

	private async Task<Dictionary<string, int[]>> GetOwnVotesAsync(IEnumerable<Poll> polls, User? user)
	{
		if (user == null) return [];
		return await db.PollVotes
		               .Where(p => polls.Select(i => i.NoteId).Any(i => i == p.NoteId) && p.User == user)
		               .GroupBy(p => p.NoteId)
		               .ToDictionaryAsync(p => p.Key, p => p.Select(i => i.Choice).ToArray());
	}

	public async Task<IEnumerable<PollEntity>> RenderManyAsync(IEnumerable<Poll> polls, User? user)
	{
		var pollList = polls.ToList();

		var data = new PollRendererDto
		{
			OwnVotes = await GetOwnVotesAsync(pollList, user), Voted = await GetVotedAsync(pollList, user)
		};

		return await pollList.Select(p => RenderAsync(p, user, data)).AwaitAllAsync();
	}

	public class PollRendererDto
	{
		public Dictionary<string, int[]>? OwnVotes;
		public List<string>?              Voted;
	}
}
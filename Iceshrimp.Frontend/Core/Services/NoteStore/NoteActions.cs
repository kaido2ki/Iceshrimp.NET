using Iceshrimp.Frontend.Components;
using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.Services.NoteStore;

internal class NoteActions(
	ApiService api,
	ILogger<NoteActions> logger,
	StateSynchronizer stateSynchronizer,
	ComposeService composeService,
	GlobalComponentSvc globalComponentService
)
{
	private void Broadcast(NoteBase note)
	{
		stateSynchronizer.Broadcast(note);
	}

	public async Task RefetchNoteAsync(string id)
	{
		try
		{
			var res = await api.Notes.GetNoteAsync(id);
			if (res == null) return;
			Broadcast(res);
		}
		catch (ApiException e)
		{
			logger.LogError(e, "Failed to fetch note.");
		}
	}

	public async Task RefetchRemoteNoteAsync(string id)
	{
		try
		{
			var res = await api.Notes.RefetchNoteAsync(id);

			if (res != null)
			{
				if (res.Errors.Count != 0)
				{
					logger.LogError($"Failed to refetch note. {string.Join(", ", res.Errors)}");
					return;
				}

				Broadcast(res.Note);
			}
		}
		catch (ApiException e)
		{
			logger.LogError(e, "Failed to refetch note.");
		}
	}

	public async Task ToggleBookmarkAsync(NoteBase note)
	{
		if (note.Bookmarked)
		{
			try
			{
				note.Bookmarked = false;
				Broadcast(note);
				await api.Notes.UnbookmarkNoteAsync(note.Id);
			}
			catch (ApiException e)
			{
				logger.LogError(e, "Failed to unbookmark note");
				note.Bookmarked = true;
				Broadcast(note);
			}
		}
		else
		{
			try
			{
				note.Bookmarked = true;
				Broadcast(note);
				await api.Notes.BookmarkNoteAsync(note.Id);
			}
			catch (ApiException e)
			{
				logger.LogError(e, "Failed to bookmark note");
				note.Bookmarked = false;
				Broadcast(note);
			}
		}
	}

	public async Task TogglePinnedAsync(NoteBase note)
	{
		if (note.Pinned)
		{
			try
			{
				note.Pinned = false;
				Broadcast(note);
				await api.Notes.UnpinNoteAsync(note.Id);
			}
			catch (ApiException e)
			{
				logger.LogError(e, "Failed to unpin note");
				note.Pinned = true;
				Broadcast(note);
			}
		}
		else
		{
			try
			{
				note.Pinned = true;
				Broadcast(note);
				await api.Notes.PinNoteAsync(note.Id);
			}
			catch (ApiException e)
			{
				logger.LogError(e, "Failed to pin note");
				note.Pinned = false;
				Broadcast(note);
			}
		}
	}

	public async Task ToggleLikeAsync(NoteBase note)
	{
		if (note.Liked)
		{
			try
			{
				note.Likes -= 1;
				note.Liked =  false;
				Broadcast(note);
				var res                         = await api.Notes.UnlikeNoteAsync(note.Id);
				if (res is not null) { note.Likes = (int)res.Value;}
				Broadcast(note);
			}
			catch (ApiException e)
			{
				logger.LogError(e, "Failed to like note");
				note.Likes += 1;
				note.Liked =  true;
				Broadcast(note);
			}
		}
		else
		{
			try
			{
				note.Likes += 1;
				note.Liked =  true;
				Broadcast(note);
				var res                         = await api.Notes.LikeNoteAsync(note.Id);
				if (res is not null) note.Likes = (int)res.Value;
				Broadcast(note);
			}
			catch (ApiException e)
			{
				logger.LogError(e, "Failed to like note");
				note.Likes -= 1;
				note.Liked =  false;
				Broadcast(note);
			}
		}
	}

	public void Reply(NoteBase note)
	{
		composeService.ComposeDialog?.OpenDialog(note);
	}

	public void DoQuote(NoteBase note)
	{
		composeService.ComposeDialog?.OpenDialog(null, note);
	}
	
	public void Edit(NoteBase note)
	{
		composeService.ComposeDialog?.OpenDialogEdit(note);
	}

	public async Task BiteAsync(NoteBase note)
	{
		await api.Notes.BiteNoteAsync(note.Id);
	}

	public async Task MuteAsync(NoteBase note)
	{
		await api.Notes.MuteNoteAsync(note.Id);
	}

	public async Task RedraftAsync(NoteBase note)
	{
		try
		{
			var original = await api.Notes.GetNoteAsync(note.Id);
			if (original is not null)
			{
				await api.Notes.DeleteNoteAsync(note.Id);
				composeService.ComposeDialog?.OpenDialogRedraft(original);
			}
		}
		catch (ApiException e)
		{
			logger.LogError(e, "Failed to redraft");
		}
	}

	public async Task RenoteAsync(NoteBase note, NoteVisibility visibility)
	{
		note.Renotes++;
		note.Renoted = true;
		Broadcast(note);
		try
		{
			await api.Notes.RenoteNoteAsync(note.Id, visibility);
		}
		catch (ApiException)
		{
			note.Renotes--;
			note.Renoted = false;
			Broadcast(note);
		}
	}

	public async Task UnrenoteAsync(NoteBase note)
	{
		note.Renotes--;
		note.Renoted = false;
		Broadcast(note);
		try
		{
			await api.Notes.UnrenoteNoteAsync(note.Id);
		}
		catch (ApiException)
		{
			note.Renotes++;
			note.Renoted = true;
			Broadcast(note);
		}
	}

	public void React(NoteBase target, EmojiResponse emoji)
	{
		var x                          = target.Reactions.FirstOrDefault(p => p.Name == emoji.Name);
		if (x is null || !x.Reacted) _ = AddReactAsync(target, emoji.Name, emoji.Sensitive, emoji.PublicUrl);
	}

	public void React(NoteBase target, string emoji)
	{
		var x                          = target.Reactions.FirstOrDefault(p => p.Name == emoji);
		if (x is null || !x.Reacted) _ = AddReactAsync(target, emoji, false, null);
	}

	public async Task AddReactAsync(NoteBase target, string name, bool sensitive, string? url = null)
	{
		var x = target.Reactions.FirstOrDefault(p => p.Name == name);
		if (x == null)
		{
			target.Reactions.Add(new NoteReactionSchema
			{
				NoteId    = target.Id,
				Name      = name,
				Count     = 1,
				Reacted   = true,
				Url       = url,
				Sensitive = sensitive
			});
		}
		else x.Count++;

		Broadcast(target);
		try
		{
			await api.Notes.ReactToNoteAsync(target.Id, name);
		}
		catch (ApiException)
		{
			if (x!.Count > 1) x.Count--;
			else target.Reactions.Remove(x);
			Broadcast(target);
		}
	}

	public async Task RemoveReactAsync(NoteBase target, string name)
	{
		var rollback = target.Reactions.First(p => p.Name == name);
		if (rollback.Count > 1) rollback.Count--;
		else target.Reactions.Remove(rollback);
		Broadcast(target);
		try
		{
			await api.Notes.RemoveReactionFromNoteAsync(target.Id, name);
		}
		catch (ApiException)
		{
			if (rollback.Count >= 1) rollback.Count++;
			else target.Reactions.Add(rollback);
			Broadcast(target);
		}
	}

	public async Task AddPollVoteAsync(NotePollSchema target, List<int> choices)
	{
		var res = await api.Notes.AddPollVoteAsync(target.NoteId, choices);
		if (res != null)
		{
			await RefetchNoteAsync(target.NoteId);
		}

	}
	
	public async Task DeleteAsync(NoteBase note)
	{
		await api.Notes.DeleteNoteAsync(note.Id);
	}

	public async Task ReportAsync(NoteBase note)
	{
		await globalComponentService.ReportDialog?.ReportNote(note)!;
	}
}

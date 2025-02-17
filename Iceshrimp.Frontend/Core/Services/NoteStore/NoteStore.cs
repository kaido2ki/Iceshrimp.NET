using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.Services.NoteStore;

internal class NoteStore : NoteMessageProvider, IDisposable
{
	public event EventHandler<NoteResponse>?                                AnyNoteChanged;
	public event EventHandler<NoteResponse>?                                AnyNoteDeleted;
	private          Dictionary<string, NoteResponse>                       Notes { get; } = [];
	private readonly StateSynchronizer                                      _stateSynchronizer;
	private readonly ApiService                                             _api;
	private readonly ILogger<NoteStore>                                     _logger;

	public NoteStore(ApiService api, ILogger<NoteStore> logger, StateSynchronizer stateSynchronizer)
	{
		_api                           =  api;
		_logger                        =  logger;
		_stateSynchronizer             =  stateSynchronizer;
		_stateSynchronizer.NoteChanged += OnNoteChanged;
	}
	//ToDo: This and similar implementations are functional but not performant, and need to be refactored
	private void OnNoteChanged(object? _, NoteBase noteResponse)
	{
		if (Notes.TryGetValue(noteResponse.Id, out var note))
		{
			note.Text        = noteResponse.Text;
			note.Cw          = noteResponse.Cw;
			note.Emoji       = noteResponse.Emoji;
			note.Liked       = noteResponse.Liked;
			note.Likes       = noteResponse.Likes;
			note.Renotes     = noteResponse.Renotes;
			note.Replies     = noteResponse.Replies;
			note.Attachments = noteResponse.Attachments;
			note.Reactions   = noteResponse.Reactions;
			note.Poll        = noteResponse.Poll;
			
			AnyNoteChanged?.Invoke(this, note);
			NoteChangedHandlers.First(p => p.Key == note.Id).Value.Invoke(this, note);
		}

		var hasReply = Notes.Where(p => p.Value.Reply?.Id == noteResponse.Id);
		foreach (var el in hasReply)
		{
			if (el.Value.Reply != null)
			{
				el.Value.Reply.Text        = noteResponse.Text;
				el.Value.Reply.Cw          = noteResponse.Cw;
				el.Value.Reply.Emoji       = noteResponse.Emoji;
				el.Value.Reply.Liked       = noteResponse.Liked;
				el.Value.Reply.Likes       = noteResponse.Likes;
				el.Value.Reply.Renotes     = noteResponse.Renotes;
				el.Value.Reply.Replies     = noteResponse.Replies;
				el.Value.Reply.Attachments = noteResponse.Attachments;
				el.Value.Reply.Reactions   = noteResponse.Reactions;
				el.Value.Reply.Poll        = noteResponse.Poll;
				NoteChangedHandlers.FirstOrDefault(p => p.Key == el.Value.Reply.Id).Value?.Invoke(this, el.Value.Reply);
			}
		}

		var hasRenote = Notes.Where(p => p.Value.Renote?.Id == noteResponse.Id);
		foreach (var el in hasRenote)
		{
			if (el.Value.Renote != null)
			{
				el.Value.Renote.Text        = noteResponse.Text;
				el.Value.Renote.Cw          = noteResponse.Cw;
				el.Value.Renote.Emoji       = noteResponse.Emoji;
				el.Value.Renote.Liked       = noteResponse.Liked;
				el.Value.Renote.Likes       = noteResponse.Likes;
				el.Value.Renote.Renotes     = noteResponse.Renotes;
				el.Value.Renote.Replies     = noteResponse.Replies;
				el.Value.Renote.Attachments = noteResponse.Attachments;
				el.Value.Renote.Reactions   = noteResponse.Reactions;
				el.Value.Renote.Poll        = noteResponse.Poll;
				NoteChangedHandlers.FirstOrDefault(p => p.Key == el.Value.Renote.Id).Value?.Invoke(this, el.Value.Renote);
			}
		}
	}
	public void Delete(string id)
	{
		if (Notes.TryGetValue(id, out var note))
		{
			AnyNoteDeleted?.Invoke(this, note);
		}
	}

	public void Dispose()
	{
		_stateSynchronizer.NoteChanged -= OnNoteChanged;
	}

	private async Task<NoteResponse?> FetchNoteAsync(string id)
	{
		try
		{
			var res = await _api.Notes.GetNoteAsync(id);
			if (res is null) return null;
			var success = Notes.TryAdd(res.Id, res);
			if (success) return res;
			Notes.Remove(res.Id);
			Notes.Add(res.Id, res);

			return res;
		}
		catch (ApiException e)
		{
			_logger.LogError(e, "Failed to fetch note.");
			throw;
		}
	}

	public async Task<NoteResponse?> GetNoteAsync(string id)
	{
		var res = Notes.TryGetValue(id, out var value);
		if (res) return value ?? throw new InvalidOperationException("Key somehow had no associated value.");
		return await FetchNoteAsync(id);
	}
}

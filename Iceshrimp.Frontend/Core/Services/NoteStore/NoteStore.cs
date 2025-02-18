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

using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.Services.NoteStore;

internal class RelatedStore : NoteMessageProvider, IDisposable
{
	private Dictionary<string, NoteContainer>          Ascendants { get; set; } = new();
	public event EventHandler<NoteResponse>?           NoteChanged;
	private          Dictionary<string, NoteContainer> Descendants { get; set; } = new();
	private readonly StateSynchronizer                 _stateSynchronizer;
	private readonly ApiService                        _api;
	private readonly ILogger<NoteStore>                _logger;

	public RelatedStore(StateSynchronizer stateSynchronizer, ApiService api, ILogger<NoteStore> logger)
	{
		_stateSynchronizer             =  stateSynchronizer;
		_api                           =  api;
		_logger                        =  logger;
		_stateSynchronizer.NoteChanged += OnNoteChanged;
	}

	private void OnNoteChanged(object? _, NoteBase noteResponse)
	{
		foreach (var container in Ascendants)
		{
			if (container.Value.Notes.TryGetValue(noteResponse.Id, out var note))
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

				NoteChangedHandlers.First(p => p.Key == note.Id).Value.Invoke(this, note);
				NoteChanged?.Invoke(this, note);
			}
		}

		foreach (var container in Descendants)
		{
			foreach (var note in container.Value.Notes)
			{
				RecursivelyUpdate(note.Value, noteResponse);
			}
		}
	}

	private void RecursivelyUpdate(NoteResponse input, NoteBase updated)
	{
		if (input.Id == updated.Id)
		{
			input.Text        = updated.Text;
			input.Cw          = updated.Cw;
			input.Emoji       = updated.Emoji;
			input.Liked       = updated.Liked;
			input.Likes       = updated.Likes;
			input.Renotes     = updated.Renotes;
			input.Replies     = updated.Replies;
			input.Attachments = updated.Attachments;
			input.Reactions   = updated.Reactions;
			input.Poll        = updated.Poll;
			var handler = NoteChangedHandlers.FirstOrDefault(p => p.Key == input.Id);
			handler.Value?.Invoke(this, input);
			NoteChanged?.Invoke(this, input);
		}

		if (input.Descendants != null)
			foreach (var descendant in input.Descendants)
			{
				RecursivelyUpdate(descendant, updated);
			}
	}

	public async Task<SortedList<string, NoteResponse>?> GetAscendantsAsync(string id, int? limit)
	{
		var success = Ascendants.TryGetValue(id, out var value);
		if (success) return value?.Notes ?? throw new InvalidOperationException("Key somehow had no associated value.");
		return await FetchAscendantsAsync(id, limit);
	}

	public async Task<SortedList<string, NoteResponse>?> GetDescendantsAsync(
		string id, int? limit, bool refresh = false
	)
	{
		if (refresh) return await FetchDescendantsAsync(id, limit);
		var success = Descendants.TryGetValue(id, out var value);
		if (success)
			return value?.Notes ?? throw new InvalidOperationException("Key somehow had no associated value.");
		return await FetchDescendantsAsync(id, limit);
	}

	private async Task<SortedList<string, NoteResponse>?> FetchAscendantsAsync(string id, int? limit)
	{
		try
		{
			var res = await _api.Notes.GetNoteAscendantsAsync(id, limit);
			if (res is null) return null;
			Ascendants.Remove(id);
			var container = new NoteContainer();
			foreach (var note in res)
			{
				container.Notes.Add(note.Id, note);
			}

			Ascendants.Add(id, container);

			return container.Notes;
		}
		catch (ApiException e)
		{
			_logger.LogError(e, "Failed to fetch note.");
			throw;
		}
	}

	private async Task<SortedList<string, NoteResponse>?> FetchDescendantsAsync(string id, int? limit)
	{
		try
		{
			var res = await _api.Notes.GetNoteDescendantsAsync(id, limit);
			if (res is null) return null;
			Descendants.Remove(id);
			var container = new NoteContainer();
			foreach (var note in res)
			{
				container.Notes.Add(note.Id, note);
			}

			Descendants.Add(id, container);

			return container.Notes;
		}
		catch (ApiException e)
		{
			_logger.LogError(e, "Failed to fetch note.");
			throw;
		}
	}

	private class NoteContainer
	{
		public SortedList<string, NoteResponse> Notes { get; } = new();
	}

	public void Dispose()
	{
		_stateSynchronizer.NoteChanged -= OnNoteChanged;
	}
}

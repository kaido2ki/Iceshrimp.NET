using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Frontend.Enums;
using Iceshrimp.Shared.Schemas.SignalR;
using Iceshrimp.Shared.Schemas.Web;
using NoteEvent = (Iceshrimp.Shared.Schemas.SignalR.StreamingTimeline timeline, Iceshrimp.Shared.Schemas.Web.NoteResponse note);

namespace Iceshrimp.Frontend.Core.Services.NoteStore;

internal class TimelineStore : NoteMessageProvider, IAsyncDisposable, IStreamingItemProvider<NoteResponse>
{
	public event EventHandler<NoteResponse>?           ItemPublished;
	private          Dictionary<string, TimelineState> Timelines { get; set; } = new();
	private readonly ApiService                        _api;
	private readonly ILogger<TimelineStore>            _logger;
	private readonly StateSynchronizer                 _stateSynchronizer;
	private readonly StreamingService                  _streamingService;

	public TimelineStore(ApiService api, ILogger<TimelineStore> logger, StateSynchronizer stateSynchronizer, StreamingService streamingService)
	{
		_api                            =  api;
		_logger                         =  logger;
		_stateSynchronizer              =  stateSynchronizer;
		_streamingService               =  streamingService;
		_stateSynchronizer.NoteChanged  += OnNoteChanged;
		_streamingService.NotePublished += OnNotePublished;
	}

	private void OnNoteChanged(object? _, NoteBase changedNote)
	{
		foreach (var timeline in Timelines)
		{
			var replies = timeline.Value.Timeline.Where(p => p.Value.Reply?.Id == changedNote.Id);
			foreach (var el in replies)
			{
				if (el.Value.Reply is null) throw new Exception("Reply in note to be modified was null");
				el.Value.Reply.Cw          = changedNote.Cw;
				el.Value.Reply.Text        = changedNote.Text;
				el.Value.Reply.Emoji       = changedNote.Emoji;
				el.Value.Reply.Liked       = changedNote.Liked;
				el.Value.Reply.Likes       = changedNote.Likes;
				el.Value.Reply.Renotes     = changedNote.Renotes;
				el.Value.Reply.Replies     = changedNote.Replies;
				el.Value.Reply.Attachments = changedNote.Attachments;
				el.Value.Reply.Reactions   = changedNote.Reactions;
				el.Value.Reply.Poll        = changedNote.Poll;

			}
				
			if (timeline.Value.Timeline.TryGetValue(changedNote.Id, out var note))
			{
				note.Cw          = changedNote.Cw;
				note.Text        = changedNote.Text;
				note.Emoji       = changedNote.Emoji;
				note.Liked       = changedNote.Liked;
				note.Likes       = changedNote.Likes;
				note.Renotes     = changedNote.Renotes;
				note.Replies     = changedNote.Replies;
				note.Attachments = changedNote.Attachments;
				note.Reactions   = changedNote.Reactions;
				note.Poll        = changedNote.Poll;

				var handler = NoteChangedHandlers.FirstOrDefault(p => p.Key == note.Id);
				handler.Value?.Invoke(this, note);
			}
		}
	}

	private async Task<List<NoteResponse>?> FetchTimelineAsync(string timeline, PaginationQuery pq)
	{
		try
		{
			var res = await _api.Timelines.GetHomeTimelineAsync(pq);
			if (Timelines.ContainsKey(timeline) is false)
			{
				Timelines.Add(timeline, new TimelineState());
			}

			foreach (var note in res)
			{
				var add = Timelines[timeline].Timeline.TryAdd(note.Id, note);
				if (add is false) _logger.LogError($"Duplicate note: {note.Id}");
			}

			return res;
		}
		catch (ApiException e)
		{
			_logger.LogError(e, "Failed to fetch timeline");
			return null;
		}
	}

	public async Task<List<NoteResponse>?> GetHomeTimelineAsync(string timeline, Cursor cs)
	{
		if (cs.Id is null)
		{
			return await FetchTimelineAsync(timeline,
											new PaginationQuery { MaxId = null, MinId = null, Limit = cs.Count });
		}

		switch (cs.Direction)
		{
			case DirectionEnum.Newer:
			{
				var indexStart = Timelines[timeline].Timeline.IndexOfKey(cs.Id);
				if (indexStart != -1 && indexStart - cs.Count > 0)
				{
					var res = Timelines[timeline]
							  .Timeline.Take(new Range(indexStart - cs.Count, indexStart));
					return res.Select(p => p.Value).ToList();
				}
				else
				{
					var res = await FetchTimelineAsync(timeline,
													   new PaginationQuery
													   {
														   MaxId = null, MinId = cs.Id, Limit = cs.Count
													   });
					res?.Reverse();
					return res;
				}
			}
			case DirectionEnum.Older:
			{
				if (!Timelines.ContainsKey(timeline))
				{
					return await FetchTimelineAsync(timeline,
													new PaginationQuery
													{
														MaxId = cs.Id, MinId = null, Limit = cs.Count
													});
				}

				var indexStart = Timelines[timeline].Timeline.IndexOfKey(cs.Id);
				if (indexStart != -1 && indexStart + cs.Count < Timelines[timeline].Timeline.Count)
				{
					var res = Timelines[timeline]
							  .Timeline.Take(new Range(indexStart, indexStart + cs.Count));
					return res.Select(p => p.Value).ToList();
				}
				else
				{
					return await FetchTimelineAsync(timeline,
													new PaginationQuery
													{
														MaxId = cs.Id, MinId = null, Limit = cs.Count
													});
				}
			}
		}

		throw new InvalidOperationException();
	}

	public List<NoteResponse> GetIdsFromTimeline(string timeline, List<string> ids)
	{
		List<NoteResponse> list = [];
		list.AddRange(ids.Select(id => Timelines[timeline].Timeline[id]));
		return list;
	}

	private void OnNotePublished(object? sender, NoteEvent valueTuple)
	{
		var (timeline, response) = valueTuple;
		if (timeline == StreamingTimeline.Home)
		{
			var success = Timelines.TryGetValue("home", out var home);
			if (success)
			{
				var add = home!.Timeline.TryAdd(response.Id, response);
				if (add is false) _logger.LogError($"Duplicate note: {response.Id}");
			}
			ItemPublished?.Invoke(this, response);
		}
	}

	public class Cursor
	{
		public required DirectionEnum Direction { get; set; }
		public required int           Count     { get; set; }
		public          string?       Id        { get; set; }
	}

	public void Dispose()
	{
		_stateSynchronizer.NoteChanged -= OnNoteChanged;
	}

	public async ValueTask DisposeAsync()
	{
		await _stateSynchronizer.DisposeAsync();
		await _streamingService.DisposeAsync();
	}

}

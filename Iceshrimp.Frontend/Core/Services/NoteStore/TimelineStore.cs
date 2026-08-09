using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Frontend.Enums;
using Iceshrimp.Shared.Helpers;
using Iceshrimp.Shared.Schemas.Web;
using NoteEvent =
	(Iceshrimp.Shared.Helpers.TimelineEnum timeline, Iceshrimp.Shared.Schemas.Web.NoteResponse note);

namespace Iceshrimp.Frontend.Core.Services.NoteStore;

internal class TimelineStore : NoteMessageProvider, IAsyncDisposable, IStreamingItemProvider<NoteResponse>
{
	public event EventHandler<NoteResponse>?           ItemPublished;
	private          Dictionary<string, TimelineState> Timelines { get; set; } = new();
	private readonly ApiService                        _api;
	private readonly ILogger<TimelineStore>            _logger;
	private readonly SessionService                    _sessionService;
	private readonly SettingsService                   _settingsService;
	private readonly StateSynchronizer                 _stateSynchronizer;
	private readonly StreamingService                  _streamingService;
	private readonly FilterService                     _filterService;

	public TimelineStore(
		ApiService api, ILogger<TimelineStore> logger, StateSynchronizer stateSynchronizer,
		SessionService sessionService, SettingsService settingsService, StreamingService streamingService,
		FilterService filterService
	)
	{
		_api                            =  api;
		_logger                         =  logger;
		_sessionService                 =  sessionService;
		_settingsService                =  settingsService;
		_stateSynchronizer              =  stateSynchronizer;
		_streamingService               =  streamingService;
		_filterService                  =  filterService;
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
				el.Value.Reply.Bookmarked  = changedNote.Bookmarked;
				el.Value.Reply.Liked       = changedNote.Liked;
				el.Value.Reply.Likes       = changedNote.Likes;
				el.Value.Reply.Renotes     = changedNote.Renotes;
				el.Value.Reply.Replies     = changedNote.Replies;
				el.Value.Reply.Attachments = changedNote.Attachments;
				el.Value.Reply.Reactions   = changedNote.Reactions;
				el.Value.Reply.Poll        = changedNote.Poll;
				NoteChangedHandlers.FirstOrDefault(p => p.Key == el.Value.Reply.Id).Value?.Invoke(this, el.Value.Reply);
			}

			var hasRenote = timeline.Value.Timeline.Where(p => p.Value.Renote?.Id == changedNote.Id);
			foreach (var el in hasRenote)
			{
				if (el.Value.Renote != null)
				{
					el.Value.Renote.Text        = changedNote.Text;
					el.Value.Renote.Cw          = changedNote.Cw;
					el.Value.Renote.Emoji       = changedNote.Emoji;
					el.Value.Renote.Bookmarked  = changedNote.Bookmarked;
					el.Value.Renote.Liked       = changedNote.Liked;
					el.Value.Renote.Likes       = changedNote.Likes;
					el.Value.Renote.Renotes     = changedNote.Renotes;
					el.Value.Renote.Replies     = changedNote.Replies;
					el.Value.Renote.Attachments = changedNote.Attachments;
					el.Value.Renote.Reactions   = changedNote.Reactions;
					el.Value.Renote.Poll        = changedNote.Poll;
					NoteChangedHandlers.FirstOrDefault(p => p.Key == el.Value.Renote.Id)
									   .Value?.Invoke(this, el.Value.Renote);
				}
			}

			if (timeline.Value.Timeline.TryGetValue(changedNote.Id, out var note))
			{
				note.Cw          = changedNote.Cw;
				note.Text        = changedNote.Text;
				note.Emoji       = changedNote.Emoji;
				note.Bookmarked  = changedNote.Bookmarked;
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

	private async Task<List<NoteResponse>?> FetchTimelineAsync(
		Timeline timeline, PaginationQuery pq
	)
	{
		try
		{
			var res = timeline.Enum switch
			{
				TimelineEnum.Home      => await _api.Timelines.GetHomeTimelineAsync(pq),
				TimelineEnum.Local     => await _api.Timelines.GetLocalTimelineAsync(pq),
				TimelineEnum.Social    => await _api.Timelines.GetSocialTimelineAsync(pq),
				TimelineEnum.Bubble    => await _api.Timelines.GetBubbleTimelineAsync(pq),
				TimelineEnum.Global    => await _api.Timelines.GetGlobalTimelineAsync(pq),
				TimelineEnum.Bookmarks => await _api.Timelines.GetBookmarksTimelineAsync(pq),
				TimelineEnum.Remote    => await _api.Timelines.GetRemoteTimelineAsync(timeline.Remote!, pq),
				TimelineEnum.Tag	   => await _api.Timelines.GetTagTimelineAsync(timeline.Tag!, pq),
				_                      => throw new ArgumentOutOfRangeException(nameof(timeline), timeline, null)
			};

			if (Timelines.ContainsKey(timeline.Key) is false)
			{
				Timelines.Add(timeline.Key, new TimelineState());
			}

			foreach (var note in res)
			{
				if (note.Filtered == null)
					_filterService.FilterNote(note,
					                          timeline.Enum == TimelineEnum.Home
						                          ? FilterResponse.FilterContext.Home
						                          : FilterResponse.FilterContext.Public);

				if (note is { Filtered.Hide: true }) continue;

				var add = Timelines[timeline.Key].Timeline.TryAdd(note.Id, note);
				if (add is false) _logger.LogWarning($"Duplicate note: {note.Id}");
			}

			res.RemoveAll(p => p.Filtered is { Hide: true });

			if (_settingsService.Preferences.Wellbeing.HideRenotes)
				res.RemoveAll(p => p.RenoteId != null && p.User.Id != _sessionService.Current?.Id);

			if (_settingsService.Preferences.Wellbeing.HideQuotes)
				res.RemoveAll(p => p.QuoteId != null && p.User.Id != _sessionService.Current?.Id);

			return res;
		}
		catch (Exception e)
		{
			_logger.LogError(e, "Failed to fetch timeline");
			return null;
		}
	}

	public async Task<List<NoteResponse>?> GetTimelineAsync(Timeline timeline, Cursor cs)
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
				var indexStart = Timelines[timeline.Key].Timeline.IndexOfKey(cs.Id);
				if (indexStart != -1 && indexStart - cs.Count > 0)
				{
					var res = Timelines[timeline.Key]
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
				if (!Timelines.ContainsKey(timeline.Key))
				{
					return await FetchTimelineAsync(timeline,
													new PaginationQuery
													{
														MaxId = cs.Id, MinId = null, Limit = cs.Count
													});
				}

				var indexStart = Timelines[timeline.Key].Timeline.IndexOfKey(cs.Id);
				if (indexStart != -1 && indexStart + cs.Count < Timelines[timeline.Key].Timeline.Count)
				{
					var res = Timelines[timeline.Key]
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

	public List<NoteResponse> GetIdsFromTimeline(Timeline timeline, List<string> ids)
	{
		var list = Timelines.TryGetValue(timeline.Key, out var timelineState)
			? ids.Select(id => timelineState.Timeline.GetValueOrDefault(id)).OfType<NoteResponse>().ToList()
			: [];

		if (_settingsService.Preferences.Wellbeing.HideRenotes)
			list.RemoveAll(p => p.RenoteId != null && p.User.Id != _sessionService.Current?.Id);

		if (_settingsService.Preferences.Wellbeing.HideQuotes)
			list.RemoveAll(p => p.QuoteId != null && p.User.Id != _sessionService.Current?.Id);

		return list;
	}

	private void OnNotePublished(object? sender, NoteEvent valueTuple)
	{
		var (timeline, response) = valueTuple;
		var targetTimeline = timeline switch
		{
			TimelineEnum.Home   => new Timeline(TimelineEnum.Home),
			TimelineEnum.Local  => new Timeline(TimelineEnum.Local),
			TimelineEnum.Social => new Timeline(TimelineEnum.Social),
			TimelineEnum.Bubble => new Timeline(TimelineEnum.Social),
			TimelineEnum.Global => new Timeline(TimelineEnum.Social),
			TimelineEnum.Remote => new Timeline(TimelineEnum.Remote),
			_                        => throw new ArgumentOutOfRangeException()
		};
		var success = Timelines.TryGetValue(targetTimeline.Key, out var selected);
		if (success)
		{
			if (response.Filtered == null)
				_filterService.FilterNote(response,
				                          timeline == TimelineEnum.Home
					                          ? FilterResponse.FilterContext.Home
					                          : FilterResponse.FilterContext.Public);

			if (response is { Filtered.Hide: true }) return;

			var add = selected!.Timeline.TryAdd(response.Id, response);
			if (add is false) _logger.LogWarning($"Duplicate note: {response.Id}");
		}

		ItemPublished?.Invoke(this, response);

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

	public class Timeline
	{
		public string       Key  { get; }
		public TimelineEnum Enum { get; }
		
		public string? Remote { get; }
		public string? Tag    { get; }

		public Timeline(TimelineEnum timelineEnum, string? instance = null)
		{
			Enum = timelineEnum;
			if (timelineEnum == TimelineEnum.Remote)
			{
				Key    = $"remote:{instance!}";
				Remote = instance ?? throw new ArgumentException("Cannot create remote key without instance");
			}
			else if (timelineEnum == TimelineEnum.Tag)
			{
				Key = $"tag:{instance!}";
				Tag = instance ?? throw new ArgumentException("Cannot create tag key without tag");
			}
			else
			{
				Key = timelineEnum.ToString();
			}
		}
	}
}

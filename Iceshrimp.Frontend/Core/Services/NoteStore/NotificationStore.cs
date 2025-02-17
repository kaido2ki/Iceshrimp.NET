using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.Services.NoteStore;

// This is presently a very thin shim to get Notification working, notifications should be transitioned to the virtual scroller eventually.
internal class NotificationStore : NoteMessageProvider, IAsyncDisposable
{
	public event EventHandler<NotificationResponse>?          Notification;
	private readonly StateSynchronizer                        _stateSynchronizer;
	private readonly ApiService                               _api;
	private readonly ILogger<NotificationStore>               _logger;
	private          StreamingService                         _streamingService;
	private          bool                                     _initialized;
	private          SortedList<string, NotificationResponse> Notifications { get; set; } = new();

	public NotificationStore(
		ApiService api, ILogger<NotificationStore> logger, StateSynchronizer stateSynchronizer,
		StreamingService streamingService
	)
	{
		_api                           =  api;
		_logger                        =  logger;
		_stateSynchronizer             =  stateSynchronizer;
		_streamingService              =  streamingService;
		_stateSynchronizer.NoteChanged += OnNoteChanged;
		_streamingService.Notification += OnNotification;
	}

	public async Task InitializeAsync()
	{
		if (_initialized) return;
		await _streamingService.ConnectAsync();
		_initialized = true;
	}

	public async ValueTask DisposeAsync()
	{
		_stateSynchronizer.NoteChanged -= OnNoteChanged;
		await _streamingService.DisposeAsync();
	}

	private void OnNotification(object? _, NotificationResponse notificationResponse)
	{
		var add = Notifications.TryAdd(notificationResponse.Id, notificationResponse);
		if (add is false) _logger.LogError($"Duplicate notification: {notificationResponse.Id}");
		Notification?.Invoke(this, notificationResponse);
	}

	public async Task<List<NotificationResponse>?> FetchNotificationsAsync(PaginationQuery pq)
	{
		try
		{
			var res = await _api.Notifications.GetNotificationsAsync(pq);
			foreach (var notification in res)
			{
				var add = Notifications.TryAdd(notification.Id, notification);
				if (add is false) _logger.LogError($"Duplicate notification: {notification.Id}");
			}

			return res;
		}
		catch (ApiException e)
		{
			_logger.LogError(e, "Failed to fetch notifications");
			return null;
		}
	}

	public void OnNoteChanged(object? _, NoteBase noteResponse)
	{
		var elements = Notifications.Where(p => p.Value.Note?.Id == noteResponse.Id);
		foreach (var el in elements)
		{
			if (el.Value.Note is null) throw new Exception("Reply in note to be modified was null");
			el.Value.Note.Cw          = noteResponse.Cw;
			el.Value.Note.Text        = noteResponse.Text;
			el.Value.Note.Emoji       = noteResponse.Emoji;
			el.Value.Note.Liked       = noteResponse.Liked;
			el.Value.Note.Likes       = noteResponse.Likes;
			el.Value.Note.Renotes     = noteResponse.Renotes;
			el.Value.Note.Replies     = noteResponse.Replies;
			el.Value.Note.Attachments = noteResponse.Attachments;
			el.Value.Note.Reactions   = noteResponse.Reactions;
			el.Value.Note.Poll        = noteResponse.Poll;
			NoteChangedHandlers.First(p => p.Key == noteResponse.Id).Value.Invoke(this, el.Value.Note);
		}
	}
}

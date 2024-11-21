using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Frontend.Core.Services.NoteStore;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Components;

namespace Iceshrimp.Frontend.Components;

public partial class NotificationList : IDisposable
{
	private          string?                    _minId;
	private          State                      _state = State.Loading;
	[Inject] private NotificationStore           NotificationStore { get; set; } = null!;
	[Inject] private ApiService                 Api              { get; set; } = null!;
	private          List<NotificationResponse> Notifications    { get; set; } = [];

	public void Dispose()
	{
		NotificationStore.Notification -= OnNotification;
	}

	private async Task GetNotifications()
	{
		try
		{
			var res =await NotificationStore.FetchNotificationsAsync(new PaginationQuery());
			if (res is null)
			{
				_state = State.Error;
				return;
			}
			if (res.Count > 0)
			{
				Notifications = res;
				_minId        = res.Last().Id;
			}

			_state = State.Init;
		}
		catch (ApiException)
		{
			_state = State.Error;
		}
	}

	private async Task LoadMore()
	{
		var pq  = new PaginationQuery { MaxId = _minId, Limit = 20 };
		var res = await NotificationStore.FetchNotificationsAsync(pq);
		if (res is null) return;
		if (res.Count > 0)
		{
			Notifications.AddRange(res);
			_minId = res.Last().Id;
			StateHasChanged();
		}
	}

	protected override async Task OnInitializedAsync()
	{
		NotificationStore.Notification += OnNotification;
		await NotificationStore.InitializeAsync();
		await GetNotifications();
		StateHasChanged();
	}

	private void OnNotification(object? _, NotificationResponse notificationResponse)
	{
		Notifications.Insert(0, notificationResponse);
		StateHasChanged();
	}

	private enum State
	{
		Loading,
		Error,
		Init
	}
}
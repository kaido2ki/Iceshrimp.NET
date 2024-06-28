using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas;
using Microsoft.AspNetCore.Components;

namespace Iceshrimp.Frontend.Components;

public partial class NotificationList : IAsyncDisposable
{
	[Inject] private StreamingService           StreamingService { get; set; } = null!;
	[Inject] private ApiService                 Api              { get; set; } = null!;
	private          List<NotificationResponse> Notifications    { get; set; } = [];
	private          State                      _state = State.Loading;
	private          string?                    _minId;

	private enum State
	{
		Loading,
		Error,
		Init
	}

	private async Task GetNotifications()
	{
		try
		{
			var res = await Api.Notifications.GetNotifications(new PaginationQuery());
			if (res.Count > 0)
			{
				Notifications = res;
				_minId        = res.Last().Id;
				foreach (var el in res)
				{
					Console.WriteLine(el.Type);
				}
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
		var res = await Api.Notifications.GetNotifications(pq);
		if (res.Count > 0)
		{
			Notifications.AddRange(res);
			_minId = res.Last().Id;
			StateHasChanged();
		}
	}

	protected override async Task OnInitializedAsync()
	{
		StreamingService.Notification += OnNotification;
		await StreamingService.Connect();
		await GetNotifications();
		StateHasChanged();
	}

	private void OnNotification(object? _, NotificationResponse notificationResponse)
	{
		Notifications.Insert(0, notificationResponse);
		StateHasChanged();
	}

	public async ValueTask DisposeAsync()
	{
		StreamingService.Notification -= OnNotification;

		await StreamingService.DisposeAsync();
		GC.SuppressFinalize(this);
	}
}
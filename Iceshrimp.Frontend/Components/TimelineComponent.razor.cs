using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Frontend.Core.Services.StateServicePatterns;
using Iceshrimp.Shared.Schemas.SignalR;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Components;

namespace Iceshrimp.Frontend.Components;

public partial class TimelineComponent : IAsyncDisposable
{
	[Inject] private ApiService                 ApiService       { get; set; } = null!;
	[Inject] private StreamingService           StreamingService { get; set; } = null!;
	[Inject] private StateService               StateService     { get; set; } = null!;
	[Inject] private ILogger<TimelineComponent> Logger           { get; set; } = null!;

	private TimelineState   State           { get; set; } = null!;
	private State           ComponentState  { get; set; } = Core.Miscellaneous.State.Loading;
	private VirtualScroller VirtualScroller { get; set; } = null!;
	private bool            LockFetch       { get; set; }

	public async ValueTask DisposeAsync()
	{
		StreamingService.NotePublished -= OnNotePublished;
		await StreamingService.DisposeAsync();
		StateService.Timeline.SetState("home", State);
	}

	private async Task<bool> Initialize()
	{
		var pq  = new PaginationQuery { Limit = 30 };
		var res = await ApiService.Timelines.GetHomeTimelineAsync(pq);
		if (res.Count < 1)
		{
			return false;
		}

		State.MaxId    = res[0].Id;
		State.MinId    = res.Last().Id;
		State.Timeline = res;
		return true;
	}

	// Returning false means the API has no more content.
	private async Task<bool> FetchOlder()
	{
		try
		{
			if (LockFetch) return true;
			LockFetch = true;
			var pq  = new PaginationQuery { Limit = 15, MaxId = State.MinId };
			var res = await ApiService.Timelines.GetHomeTimelineAsync(pq);
			switch (res.Count)
			{
				case > 0:
					State.MinId = res.Last().Id;
					State.Timeline.AddRange(res);
					break;
				case 0:
					return false;
			}
		}
		catch (HttpRequestException)
		{
			Logger.LogError("Network Error");
			return false;
		}

		LockFetch = false;
		return true;
	}

	private async Task FetchNewer()
	{
		try
		{
			if (LockFetch) return;
			LockFetch = true;
			var pq  = new PaginationQuery { Limit = 15, MinId = State.MaxId };
			var res = await ApiService.Timelines.GetHomeTimelineAsync(pq);
			if (res.Count > 0)
			{
				State.MaxId = res.Last().Id;
				State.Timeline.InsertRange(0, res);
			}
		}
		catch (HttpRequestException)
		{
			Logger.LogError("Network Error");
		}

		LockFetch = false;
	}

	private async void OnNotePublished(object? _, (StreamingTimeline timeline, NoteResponse note) data)
	{
		State.Timeline.Insert(0, data.note);
		State.MaxId = data.note.Id;
		if (ComponentState is Core.Miscellaneous.State.Empty)
		{
			State.MinId    = data.note.Id;
			ComponentState = Core.Miscellaneous.State.Loaded;
			StateHasChanged();
		}
		else
		{
			StateHasChanged();
			await VirtualScroller.OnNewNote();
		}
	}

	protected override async Task OnInitializedAsync()
	{
		StreamingService.NotePublished += OnNotePublished;
		await StreamingService.ConnectAsync();
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			State = StateService.Timeline.GetState("home");
			var initResult = true;
			if (State.Timeline.Count == 0)
			{
				initResult = await Initialize();
			}

			ComponentState = initResult ? Core.Miscellaneous.State.Loaded : Core.Miscellaneous.State.Empty;
			StateHasChanged();
		}
	}
}
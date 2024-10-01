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
		var res = await ApiService.Timelines.GetHomeTimeline();
		if (res.Items.Count < 1)
		{
			return false;
		}

		State.PageUp = res.PageUp;
		State.PageDown = res.PageDown;
		State.Timeline = res.Items;
		return true;
	}

	// Returning false means the API has no more content.
	private async Task<bool> FetchOlder()
	{
		try
		{
			if (LockFetch) return true;
			LockFetch = true;

			var res = await ApiService.Timelines.GetHomeTimeline(State.PageDown);
			if (res.Items.Count > 0)
			{
				State.PageDown = res.PageDown;
				State.Timeline.AddRange(res.Items);
			}

			// TODO: despite this returning false it keeps trying to load new pages. bug in VirtualScroller?
			LockFetch = false;
			return res.PageDown != null;
		}
		catch (HttpRequestException)
		{
			Logger.LogError("Network Error");
		}

		LockFetch = false;
		return false;
	}

	private async Task FetchNewer()
	{
		try
		{
			if (LockFetch) return;
			LockFetch = true;

			var res = await ApiService.Timelines.GetHomeTimeline(State.PageUp);
			if (res.Items.Count > 0)
			{
				// TODO: the cursor-based pagination always sorts items the same way, the old one reversed when min_id was used
				// is it possible to modify the frontend to make this unnecessary? is the effort even worth it?
				res.Items.Reverse();

				State.PageUp = res.PageUp;
				State.Timeline.InsertRange(0, res.Items);
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
		// TODO: how should the below commented out lines be handled with cursor pagination?
		// State.MaxId = data.note.Id;
		if (ComponentState is Core.Miscellaneous.State.Empty)
		{
			// State.MinId    = data.note.Id;
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
		await StreamingService.Connect();
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
using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Frontend.Core.Services.StateServicePatterns;
using Iceshrimp.Shared.Schemas.SignalR;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Components;

namespace Iceshrimp.Frontend.Components;

public partial class TimelineComponent : IAsyncDisposable
{
	private          bool             _init = false;
	[Inject] private ApiService       ApiService       { get; set; } = null!;
	[Inject] private StreamingService StreamingService { get; set; } = null!;
	[Inject] private StateService     StateService     { get; set; } = null!;

	private TimelineState State { get; set; } = new()
	{
		Timeline = [],
		MaxId    = null,
		MinId    = null
	};

	private VirtualScroller VirtualScroller { get; set; } = null!;
	private bool            LockFetch       { get; set; }

	public async ValueTask DisposeAsync()
	{
		StreamingService.NotePublished -= OnNotePublished;
		await StreamingService.DisposeAsync();
	}

	private async Task Initialize()
	{
		var pq  = new PaginationQuery { Limit = 30 };
		var res = await ApiService.Timelines.GetHomeTimeline(pq);
		State.MaxId    = res[0].Id;
		State.MinId    = res.Last().Id;
		State.Timeline = res;
	}

	// Returning false means the API has no more content.
	private async Task<bool> FetchOlder()
	{
		if (LockFetch) return true;
		LockFetch = true;
		var pq  = new PaginationQuery { Limit = 15, MaxId = State.MinId };
		var res = await ApiService.Timelines.GetHomeTimeline(pq);
		switch (res.Count)
		{
			case > 0:
				State.MinId = res.Last().Id;
				State.Timeline.AddRange(res);
				break;
			case 0:
				return false;
		}

		LockFetch = false;
		return true;
	}

	private async Task FetchNewer()
	{
		if (LockFetch) return;
		LockFetch = true;
		var pq  = new PaginationQuery { Limit = 15, MinId = State.MaxId };
		var res = await ApiService.Timelines.GetHomeTimeline(pq);
		if (res.Count > 0)
		{
			State.MaxId = res.Last().Id;
			State.Timeline.InsertRange(0, res);
		}

		LockFetch = false;
	}

	private async void OnNotePublished(object? _, (StreamingTimeline timeline, NoteResponse note) data)
	{
		State.Timeline.Insert(0, data.note);
		State.MaxId = data.note.Id;
		StateHasChanged();
		await VirtualScroller.OnNewNote();
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
			try
			{
				var timeline = StateService.Timeline.GetState("home");
				State = timeline;
				_init = true;
				StateHasChanged();
			}
			catch (ArgumentException)
			{
				await Initialize();
				_init = true;
				StateHasChanged();
			}
		}

		StateService.Timeline.SetState("home", State);
	}
}
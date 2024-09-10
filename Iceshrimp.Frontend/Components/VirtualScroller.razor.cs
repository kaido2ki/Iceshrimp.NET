using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Frontend.Core.Services.StateServicePatterns;
using Iceshrimp.Shared.Schemas.Web;
using Ljbc1994.Blazor.IntersectionObserver;
using Ljbc1994.Blazor.IntersectionObserver.API;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

namespace Iceshrimp.Frontend.Components;

public partial class VirtualScroller : IAsyncDisposable
{
	[Inject]                     private         IIntersectionObserverService ObserverService  { get; set; } = null!;
	[Inject]                     private         IJSRuntime                   Js               { get; set; } = null!;
	[Inject]                     private         StateService                 StateService     { get; set; } = null!;
	[Inject]                     private         MessageService               MessageService   { get; set; } = null!;
	[Inject]                     private         NavigationManager            Navigation       { get; set; } = null!;
	[Parameter] [EditorRequired] public required List<NoteResponse>           NoteResponseList { get; set; }
	[Parameter] [EditorRequired] public required Func<Task<bool>>             ReachedEnd       { get; set; }
	[Parameter] [EditorRequired] public required EventCallback                ReachedStart     { get; set; }
	private                                      VirtualScrollerState         State            { get; set; } = null!;
	private                                      int                          UpdateCount      { get; set; } = 15;
	private                                      int                          _count = 30;
	private                                      List<ElementReference>       _refs  = [];
	private                                      IntersectionObserver?        OvrscrlObsvTop    { get; set; }
	private                                      IntersectionObserver?        OvrscrlObsvBottom { get; set; }
	private                                      bool                         _overscrollTop    = false;
	private                                      bool                         _overscrollBottom = false;
	private                                      ElementReference             _padTopRef;
	private                                      ElementReference             _padBotRef;
	private                                      ElementReference             _scroller;
	private                                      bool                         _loadingTop    = false;
	private                                      bool                         _loadingBottom = false;
	private                                      bool                         _setScroll     = false;
	private                                      IDisposable?                 _locationChangeHandlerDisposable;

	private ElementReference Ref
	{
		set => _refs.Add(value);
	}

	private bool               _interlock = false;
	private IJSInProcessObjectReference Module { get; set; } = null!;

	private void InitialRender(string? id)
	{
		State.RenderedList = NoteResponseList.Count < _count ? NoteResponseList : NoteResponseList.GetRange(0, _count);
	}

	public ValueTask DisposeAsync()
	{
		// SaveState();
		State.Dispose();
		_locationChangeHandlerDisposable?.Dispose();
		MessageService.AnyNoteDeleted -= OnNoteDeleted;
		return ValueTask.CompletedTask;
	}

	private async Task LoadOlder()
	{
		_loadingBottom = true;
		StateHasChanged();
		var moreAvailable = await ReachedEnd();
		if (moreAvailable == false)
		{
			if (OvrscrlObsvBottom is null) throw new Exception("Tried to use observer that does not exist");
			await OvrscrlObsvBottom.Disconnect();
		}

		_loadingBottom = false;
		StateHasChanged();
	}

	private async Task LoadNewer()
	{
		_loadingTop = true;
		StateHasChanged();
		await ReachedStart.InvokeAsync();
		_loadingTop = false;
		StateHasChanged();
	}

	private void SaveState()
	{
		GetScrollY();  // ^-^ grblll mrrp
		StateService.VirtualScroller.SetState("home", State);
	}

	private void RemoveAbove(int amount)
	{
		for (var i = 0; i < amount; i++)
		{
			var height = Module.Invoke<int>("GetHeight", _refs[i]);
			State.PadTop                           += height;
			State.Height[State.RenderedList[i].Id] =  height;
		}

		State.RenderedList.RemoveRange(0, amount);
	}

	private async Task Down()
	{
		if (OvrscrlObsvBottom is null) throw new Exception("Tried to use observer that does not exist");
		await OvrscrlObsvBottom.Disconnect();

		if (NoteResponseList.Count <= 0)
		{
			return;
		}
		
		var index = NoteResponseList.IndexOf(State.RenderedList.Last());
		if (index >= NoteResponseList.Count - (1 + UpdateCount))
		{
			await LoadOlder();
		}
		else
		{
			var a            = NoteResponseList.GetRange(index + 1, UpdateCount);
			var heightChange = 0;
			foreach (var el in a)
			{
				if (State.Height.TryGetValue(el.Id, out var value))
					heightChange += value;
			}

			if (State.PadBottom > 0) State.PadBottom -= heightChange;

			State.RenderedList.AddRange(a);
			RemoveAbove(UpdateCount);
			_interlock = false;
			StateHasChanged();
		}

		await OvrscrlObsvBottom.Observe(_padBotRef);
	}

	private async ValueTask Up(int updateCount)
	{
		if (OvrscrlObsvTop is null) throw new Exception("Tried to use observer that does not exist");
		await OvrscrlObsvTop.Disconnect();
		for (var i = 0; i < updateCount; i++)
		{
			var height = Module.Invoke<int>("GetHeight", _refs[i]);
			State.PadBottom                        += height;
			State.Height[State.RenderedList[i].Id] =  height;
		}

		var index        = NoteResponseList.IndexOf(State.RenderedList.First());
		var a            = NoteResponseList.GetRange(index - updateCount, updateCount);
		var heightChange = 0;
		foreach (var el in a)
		{
			State.Height.TryGetValue(el.Id, out var height);
			heightChange += height;
		}

		State.PadTop -= heightChange;
		State.RenderedList.InsertRange(0, a);
		State.RenderedList.RemoveRange(State.RenderedList.Count - updateCount, updateCount);
		StateHasChanged();
		_interlock = false;
		await OvrscrlObsvTop.Observe(_padTopRef);
	}

	private async Task SetupObservers()
	{
		OvrscrlObsvTop    = await ObserverService.Create(OverscrollCallbackTop);
		OvrscrlObsvBottom = await ObserverService.Create(OverscrollCallbackBottom);

		await OvrscrlObsvTop.Observe(_padTopRef);
		await OvrscrlObsvBottom.Observe(_padBotRef);
	}

	public async Task OnNewNote()
	{
		if (_overscrollTop && _interlock == false)
		{
			_interlock = true;
			await Up(1);
		}
	}

	private async void OverscrollCallbackTop(IList<IntersectionObserverEntry> list)
	{
		var entry = list.First();
		_overscrollTop = entry.IsIntersecting;

		if (_interlock == false)
		{
			var index = NoteResponseList.IndexOf(State.RenderedList.First());
			if (index == 0)
			{
				await LoadNewer();
				return;
			}

			var updateCount = UpdateCount;
			if (index < UpdateCount)
			{
				updateCount = index;
			}

			_interlock = true;
			if (list.First().IsIntersecting)

			{
				await Up(updateCount);
			}

			_interlock = false;
		}
	}

	private async void OverscrollCallbackBottom(IList<IntersectionObserverEntry> list)
	{
		var entry = list.First();
		_overscrollBottom = entry.IsIntersecting;
		if (_interlock == false)
		{
			_interlock = true;
			if (list.First().IsIntersecting)
			{
				await Down();
			}

			_interlock = false;
		}
	}

	private void GetScrollY()
	{
		var scrollTop = Module.Invoke<float>("GetScrollY");
		State.ScrollTop = scrollTop;
	}

	private void SetScrollY()
	{
		Module.InvokeVoid("SetScrollY", State.ScrollTop);
	}

	private void OnNoteDeleted(object? _, NoteResponse note)
	{
		State.RenderedList.Remove(note);
		StateHasChanged();
	}

	private ValueTask LocationChangeHandler(LocationChangingContext arg)
	{
		SaveState();
		return ValueTask.CompletedTask;
	}

	protected override void OnInitialized()
	{
		State                            =  StateService.VirtualScroller.CreateStateObject();
		MessageService.AnyNoteDeleted    += OnNoteDeleted;
		_locationChangeHandlerDisposable =  Navigation.RegisterLocationChangingHandler(LocationChangeHandler);
		try
		{
			var virtualScrollerState = StateService.VirtualScroller.GetState("home");
			State      = virtualScrollerState;
			_setScroll = true;
		}
		catch (ArgumentException)
		{
			InitialRender(null);
		}
	}

	protected override void OnParametersSet()
	{
		StateHasChanged();
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			Module = (IJSInProcessObjectReference) await Js.InvokeAsync<IJSObjectReference>("import", "./Components/VirtualScroller.razor.js");
			await SetupObservers();
		}

		if (_setScroll)
		{
			SetScrollY();
			_setScroll = false;
		}
	}
}
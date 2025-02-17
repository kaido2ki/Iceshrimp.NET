using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Frontend.Core.Services.StateServicePatterns;
using Iceshrimp.Frontend.Enums;
using Iceshrimp.Shared.Helpers;
using Ljbc1994.Blazor.IntersectionObserver;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using Microsoft.Extensions.Localization;

namespace Iceshrimp.Frontend.Components;

public class VirtualScroller<T> : ComponentBase, IDisposable where T : IIdentifiable

{
	[Inject] private IIntersectionObserverService ObserverService { get; set; } = null!;
	[Inject] private StateService State { get; set; } = null!;
	[Inject] private NavigationManager Navigation { get; set; } = null!;
	[Inject] private ILogger<NewVirtualScroller> Logger { get; set; } = null!;
	[Inject] private IJSRuntime Js { get; set; } = null!;
	[Inject] private IStringLocalizer<Iceshrimp.Frontend.Localization.Localization> Loc { get; set; } = null!;

	[Parameter] [EditorRequired] public required RenderFragment<T> ItemTemplate { get; set; } = default!;
	[Parameter] [EditorRequired] public required IReadOnlyList<T> InitialItems { get; set; } = default!;
	[Parameter] [EditorRequired] public required Func<DirectionEnum, T, Task<List<T>?>> ItemProvider { get; set; }
	[Parameter] [EditorRequired] public required string StateKey { get; set; }
	[Parameter] [EditorRequired] public required Func<List<string>, List<T>> ItemProviderById { get; set; }
	[Parameter]                  public          IStreamingItemProvider<T>? StreamingItemProvider { get; set; }
	private                                      ScrollEnd Before { get; set; } = null!;
	private                                      ScrollEnd After { get; set; } = null!;
	private                                      Dictionary<string, LazyComponent> Children { get; set; } = new();

	private SortedDictionary<string, T> Items { get; init; }
		= new(Comparer<string>.Create((x, y) => String.Compare(y, x, StringComparison.Ordinal)));

	private IJSInProcessObjectReference      _module = null!;
	private SortedDictionary<string, Child>? _stateItems;

	private float _scrollY;
	private bool  _setScroll    = false;
	private bool  _shouldRender = false;
	private bool  _initialized  = false;
	private bool  _hideBefore   = true;
	private bool  _hideAfter    = true;

	private IDisposable? _locationChangeHandlerDisposable;

	protected override bool ShouldRender()
	{
		return _shouldRender;
	}

	private void ReRender()
	{
		_shouldRender = true;
		StateHasChanged();
		_shouldRender = false;
	}

	protected override async Task OnInitializedAsync()
	{
		_module = await Js.InvokeAsync<IJSInProcessObjectReference>("import", "./Components/NewVirtualScroller.cs.js");
		_locationChangeHandlerDisposable = Navigation.RegisterLocationChangingHandler(LocationChangeHandlerAsync);
		State.NewVirtualScroller.States.TryGetValue(StateKey, out var value);
		if (value is null)
		{
			foreach (var el in InitialItems)
			{
				var x = Items.TryAdd(el.Id, el);
				if (x is false) Logger.LogWarning($"Dropped duplicate element with ID: {el.Id}");
			}
		}
		else
		{
			_stateItems = value.Items;
			var items = ItemProviderById(value.Items.Select(p => p.Value.Id).ToList());
			foreach (var el in items)
			{
				var x = Items.TryAdd(el.Id, el);
				if (x is false) Logger.LogWarning($"Dropped duplicate element with ID: {el.Id}");
			}

			_scrollY   = value.ScrollY;
			_setScroll = true;
		}

		if (StreamingItemProvider != null) StreamingItemProvider.ItemPublished += OnNewItem;

		ReRender();
		_initialized = true;
	}

	protected override void OnAfterRender(bool firstRender)
	{
		if (_setScroll)
		{
			RestoreOffset(_scrollY);
			_setScroll = false;
		}
	}

	private ValueTask LocationChangeHandlerAsync(LocationChangingContext arg)
	{
		Save();
		return ValueTask.CompletedTask;
	}

	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenRegion(1);
		builder.OpenComponent<ScrollEnd>(1);
		builder.AddComponentParameter(2, "IntersectionChange", new EventCallback(this, CallbackBeforeAsync));
		builder.AddComponentParameter(3, "ManualLoad", new EventCallback(this, CallbackBeforeAsync));
		builder.AddComponentParameter(4, "RequireReset", true);
		builder.AddComponentParameter(5, "Class", "virtual-scroller-button");
		builder.AddComponentParameter(6, "Hide", _hideBefore);
		builder.AddComponentReferenceCapture(7,
											 reference =>
												 Before = reference as ScrollEnd
														  ?? throw new InvalidOperationException());
		builder.CloseComponent();
		builder.CloseRegion();

		if (Items.Count == 0)
		{
			builder.OpenRegion(2);
			builder.OpenElement(1, "div");
			builder.AddAttribute(2, "class", "placeholder");
			builder.AddContent(3,@Loc["Nothing here, yet!"]);
			builder.CloseElement();
			builder.CloseRegion();
		}

		builder.OpenRegion(3);
		foreach (var item in Items)
		{
			builder.OpenElement(2, "div");
			builder.AddAttribute(3, "class", "target");
			builder.SetKey(item.Key);
			builder.OpenComponent<LazyComponent>(5);
			if (_stateItems != null)
			{
				var res = _stateItems.TryGetValue(item.Key, out var value);
				if (res)
				{
					builder.AddComponentParameter(6, "InitialHeight", value!.Height);
				}
			}

			builder.AddAttribute(8, "ChildContent",
								 (RenderFragment)(builder2 => { builder2.AddContent(9, ItemTemplate(item.Value)); }));
			builder.AddComponentReferenceCapture(10,
												 o => Children[item.Key] = o as LazyComponent
																		   ?? throw new InvalidOperationException());
			builder.CloseComponent();
			builder.CloseElement();
		}

		builder.CloseRegion();

		builder.OpenRegion(4);
		builder.OpenComponent<ScrollEnd>(1);
		builder.AddComponentParameter(2, "IntersectionChange", new EventCallback(this, CallbackAfterAsync));
		builder.AddComponentParameter(3, "ManualLoad", new EventCallback(this, CallbackAfterAsync));
		builder.AddComponentParameter(4, "RequireReset", true);
		builder.AddComponentParameter(5, "Class", "virtual-scroller-button");
		builder.AddComponentParameter(6, "Hide", _hideAfter);
		builder.AddComponentReferenceCapture(7,
											 reference =>
												 After = reference as ScrollEnd
														 ?? throw new InvalidOperationException());
		builder.CloseElement();
		builder.CloseRegion();
	}

	private async Task CallbackBeforeAsync()
	{
		// Cannot call item provider when there are no existing items.
		if (Items.Count == 0 || !_initialized)
		{
			Before.Reset();
			return;
		}

		var heightBefore = _module.Invoke<float>("GetDocumentHeight");
		var resTask      = ItemProvider(DirectionEnum.Newer, Items.First().Value);
		// Only show the spinner if ItemProvider takes more than 500ms to load, and the spinner is actually visible on screen.
		await Task.WhenAny(Task.Delay(TimeSpan.FromMilliseconds(500)), resTask);
		if (resTask.IsCompleted == false)
		{
			if (Before.Visible)
			{
				_hideBefore = false;
				ReRender();
			}
		}

		var res = await resTask;
		if (res is not null && res.Count > 0)
		{
			foreach (var el in res)
			{
				var x = Items.TryAdd(el.Id, el);
				if (x is false) Logger.LogWarning($"Dropped duplicate element with ID: {el.Id}");
			}

			ReRender();
			var heightAfter = _module.Invoke<float>("GetDocumentHeight");
			var diff        = heightAfter - heightBefore;
			var scroll      = _module.Invoke<float>("GetScrollY");
			_module.InvokeVoid("SetScrollY", scroll + diff);
		}
		else if (res?.Count == 0)
		{
			_hideBefore = true;
			Logger.LogInformation("Hiding before");
			ReRender();
		}

		Before.Reset();
	}

	public void OnNewItem(object? _, T item)
	{
		var height = GetScrollY();
		if (height == 0)
		{
			var add = Items.TryAdd(item.Id, item);
			if (add is false) Logger.LogWarning($"Duplicate notification: {item.Id}");
		}

		ReRender();
	}

	private async Task CallbackAfterAsync()
	{
		// Cannot call item provider when there are no existing items.
		if (Items.Count == 0 || !_initialized)
		{
			After.Reset();
			return;
		}

		var resTask = ItemProvider(DirectionEnum.Older, Items.Last().Value);
		// Only show the spinner if ItemProvider takes more than 500ms to load, and the spinner is actually visible on screen.
		await Task.WhenAny(Task.Delay(TimeSpan.FromMilliseconds(500)), resTask);
		if (resTask.IsCompleted == false)
		{
			if (After.Visible)
			{
				_hideAfter = false;
				ReRender();
			}
		}

		var res = await resTask;
		if (res is not null && res.Count > 0)
		{
			foreach (var el in res)
			{
				var x = Items.TryAdd(el.Id, el);
				if (x is false) Logger.LogWarning($"Dropped duplicate element with ID: {el.Id}");
			}
		}

		After.Reset();
		ReRender();
	}

	private float GetScrollY() // ^-^ grblll mrrp
	{
		var js      = (IJSInProcessRuntime)Js;
		var scrollY = js.Invoke<float>("GetScrollY");
		return scrollY;
	}

	private void RestoreOffset(float scrollY)
	{
		_module.InvokeVoid("SetScrollY", scrollY);
	}

	private void Save()
	{
		var scrollY = GetScrollY();
		var r =
			new SortedDictionary<string, Child>(Children.ToDictionary(p => p.Key,
																	  p => new Child
																	  {
																		  Id = p.Key, Height = p.Value.GetHeight()
																	  }));
		var x = State.NewVirtualScroller.States.TryAdd(StateKey,
													   new NewVirtualScrollerState { Items = r, ScrollY = scrollY });
		if (!x)
		{
			State.NewVirtualScroller.States.Remove(StateKey);
			State.NewVirtualScroller.States.Add(StateKey,
												new NewVirtualScrollerState { Items = r, ScrollY = scrollY });
		}
	}

	public void Dispose()
	{
		_locationChangeHandlerDisposable?.Dispose();
		if (StreamingItemProvider != null) StreamingItemProvider.ItemPublished -= OnNewItem;
	}
}

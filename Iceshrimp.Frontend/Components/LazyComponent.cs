using Ljbc1994.Blazor.IntersectionObserver;
using Ljbc1994.Blazor.IntersectionObserver.API;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Iceshrimp.Frontend.Components;

public class LazyComponent : ComponentBase, IAsyncDisposable
{
	[Inject]                     private         IIntersectionObserverService ObserverService { get; set; } = null!;
	[Inject]                     private         IJSInProcessRuntime          Js              { get; set; } = null!;
	[Inject]                     private         ILogger<LazyComponent>       Logger          { get; set; } = null!;
	[Parameter] [EditorRequired] public required RenderFragment               ChildContent    { get; set; } = default!;
	[Parameter] public long?                        InitialHeight   { get; set; }
	private                                      IntersectionObserver?        Observer        { get; set; }
	public                                       ElementReference             Target          { get; private set; }
	public                                       bool                         Visible         { get; private set; }
	private                                       long?                        Height          { get; set; }

	protected override void OnInitialized()
	{
		if (InitialHeight is not null)
		{
			Height = InitialHeight;
			Console.WriteLine($"Set Initial height to {InitialHeight}");
		}
	}

	public long? GetHeight()
	{
		if (Height != null) return Height.Value;
		if (Visible) return Js.Invoke<long>("getHeight", Target);
		else
		{
			Logger.LogError("Invisible, no height available");
			return null;
		}
	}

	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenElement(1, "div");
		builder.AddAttribute(2, "class", "target lazy-component-target-internal");
		builder.AddElementReferenceCapture(3, elementReference => Target = elementReference);

		builder.OpenRegion(10);
		if (Visible)
		{
			builder.AddContent(1, ChildContent);
		}
		else
		{
			builder.OpenElement(2, "div");
			builder.AddAttribute(3, "class", "placeholder");
			if (Height is not null)
			{
				builder.AddAttribute(4, "style", $"height: {Height}px");
			}
			else
			{
				builder.AddAttribute(5, "style", "height: 5rem");
			}

			builder.CloseElement();
		}

		builder.CloseRegion();
		builder.CloseElement();
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			Observer = await ObserverService.Create(OnIntersect);
			await Observer.Observe(Target);
		}
	}

	private void OnIntersect(IList<IntersectionObserverEntry> entries)
	{
		var entry = entries.First();
		if (Visible && entry.IsIntersecting is false)
		{
			Height  = Js.Invoke<long>("getHeight", Target);
			Visible = false;
			StateHasChanged();
		}

		if (Visible is false && entry.IsIntersecting)
		{
			Visible = true;
			StateHasChanged();
		}
	}

	public async ValueTask DisposeAsync()
	{
		if (Observer != null) await Observer.Dispose();
	}
}

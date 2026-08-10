using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Iceshrimp.Frontend.Core.Services;

internal class IntersectionService : IDisposable
{
    private readonly ILogger<IntersectionService>               _logger;
    private readonly DotNetObjectReference<IntersectionService> _dotNetObjectReference;

    private Lazy<Task<IJSObjectReference>>              Module  { get; init; }
    private ConcurrentDictionary<string, Action<Entry>> Entries { get; set; }

    public IntersectionService(IJSRuntime js, ILogger<IntersectionService> logger)
    {
        _logger                = logger;
        _dotNetObjectReference = DotNetObjectReference.Create(this);

        Module = new Lazy<Task<IJSObjectReference>>(async () =>
        {
            var module = await js.InvokeAsync<IJSObjectReference>("import", "/Core/Services/IntersectionService.js");
            await module.InvokeVoidAsync("setupObserver", _dotNetObjectReference);
            return module;
        });
        Entries = new ConcurrentDictionary<string, Action<Entry>>();
    }

    /// <summary>
    /// Register an element to be observed.
    /// </summary>
    /// <param name="element">Element to be observed.</param>
    /// <param name="action">Function to run on observation.</param>
    /// <remarks>
    /// This should be called during <see cref="ComponentBase.OnAfterRenderAsync"/> <c>firstRender</c> or when <paramref name="element"/> is otherwise guaranteed to exist. Components should also implement <see cref="IAsyncDisposable"/> and call <see cref="UnobserveAsync"/>.
    /// </remarks>
    public async Task ObserveAsync(ElementReference element, Action<Entry> action)
    {
        try
        {
            var module = await Module.Value;
            await module.InvokeVoidAsync("addEntry", element);
            Entries.AddOrUpdate(element.Id, action, (_, _) => action);
        }
        catch (JSException)
        {
            _logger.LogWarning("Failed to add {element} to observer", element.Id);
            throw;
        }
    }

    public async Task UnobserveAsync(ElementReference element)
    {
        try
        {
            var module = await Module.Value;
            await module.InvokeVoidAsync("removeEntry", element);
            Entries.Remove(element.Id, out var _);
        }
        catch (JSException)
        {
            _logger.LogWarning("Failed to remove {element} from observer", element.Id);
        }
    }

    /// <summary>
    /// This should only be called from JS.
    /// </summary>
    [JSInvokable("Observe")]
    public void ObserveFromJs(string id, Entry entry)
    {
        if (Entries.TryGetValue(id, out var action))
        {
            _logger.LogInformation("{a} {b}", entry.IsIntersecting, entry.IntersectionRatio);
            action.Invoke(entry);
        }
    }

    public void Dispose()
    {
        if (Module.IsValueCreated)
        {
            Module.Value.Dispose();
        }
    }

    public class Entry
    {
        [JsonPropertyName("intersectionRatio")]
        public double IntersectionRatio { get; set; }

        [JsonPropertyName("isIntersecting")]
        public bool IsIntersecting { get; set; }
    }
}

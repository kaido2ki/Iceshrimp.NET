using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Iceshrimp.Frontend.Core.Services;

/// <summary>
/// This service is used to wrap any calls to JS interop for functionality that is used in multiple places around the frontend (e.g. showing/closing a dialog)
/// </summary>
public class JsService
{
    public IJSObjectReference? Module { private get; set; }

    /// <summary>
    /// Displays a dialog
    /// </summary>
    /// <param name="dialog">Dialog element</param>
    /// <param name="modal">Show as a modal</param>
    public ValueTask ShowDialogAsync(ElementReference dialog, bool modal = true) =>
        Module!.InvokeVoidAsync("showDialog", dialog, modal);

    /// <summary>
    /// Closes a dialog
    /// </summary>
    /// <param name="dialog">Dialog element</param>
    /// <param name="returnValue">Return value for the dialog element</param>
    public ValueTask CloseDialogAsync(ElementReference dialog, string? returnValue = null) =>
        Module!.InvokeVoidAsync("closeDialog", dialog, returnValue);

    /// <summary>
    /// Scrolls an element into view
    /// </summary>
    /// <param name="element">Element to scroll to</param>
    /// <param name="behavior">Smoothing behavior</param>
    public ValueTask ScrollToElementAsync(ElementReference element, ScrollBehavior behavior = ScrollBehavior.Auto) =>
        Module!.InvokeVoidAsync("scrollToElement", element, behavior.ToString().ToLower());

    /// <summary>
    /// Simulates a mouse click on an element
    /// </summary>
    /// <param name="element">Element to click</param>
    public ValueTask ClickElementAsync(ElementReference element) => Module!.InvokeVoidAsync("clickElement", element); 

    /// <summary>
    /// Gets the position of an element
    /// </summary>
    /// <param name="element">Element</param>
    /// <param name="includeScroll">Include window scroll in position</param>
    /// <returns>Element X and Y coordinates</returns>
    public async ValueTask<(double, double)> GetPositionAsync(ElementReference element, bool includeScroll)
    {
        var pos = await Module!.InvokeAsync<double[]>("getPosition", element, includeScroll);
        return (pos[0], pos[1]);
    }

    /// <summary>
    /// Get the selection start of an element
    /// </summary>
    /// <param name="element">Selected element</param>
    /// <returns>Selection start index</returns>
    public ValueTask<int> GetSelectionStartAsync(ElementReference element) =>
        Module!.InvokeAsync<int>("getSelectionStart", element);

    /// <summary>
    /// Check whether sharing is supported
    /// </summary>
    /// <param name="text">Text to share</param>
    /// <param name="url">URL to share</param>
    /// <returns>Data can be shared</returns>
    public ValueTask<bool> CanShareAsync(string? text, string url) =>
        Module!.InvokeAsync<bool>("canShareLink", text, url);

    /// <summary>
    /// Share a link with optional text
    /// </summary>
    /// <param name="text">Text to share</param>
    /// <param name="url">URL to share</param>
    public ValueTask ShareAsync(string? text, string url) =>
        Module!.InvokeVoidAsync("shareLink", text, url);

    /// <summary>
    /// Toggle a popover
    /// </summary>
    /// <param name="element">Popover to toggle</param>
    /// <param name="source">Element to associate with the popover</param>
    public ValueTask TogglePopoverAsync(ElementReference element, ElementReference? source = null, bool? force = null) =>
        Module!.InvokeVoidAsync("togglePopover", element, source);

    public enum ScrollBehavior
    {
        Auto,
        Instant,
        Smooth,
    }
}

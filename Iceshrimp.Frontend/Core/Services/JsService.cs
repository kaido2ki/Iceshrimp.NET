using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Iceshrimp.Frontend.Core.Services;

/// <summary>
/// This service is used to wrap any calls to JS interop for functionality that is used in multiple places around the frontend (e.g. showing/closing a dialog)
/// </summary>
public class JsService
{
    public IJSObjectReference? Module { private get; set; }

    public ValueTask ShowDialogAsync(ElementReference dialog) => Module!.InvokeVoidAsync("showDialog", dialog);

    public ValueTask CloseDialogAsync(ElementReference dialog, string? returnValue = null) => Module!.InvokeVoidAsync("closeDialog", dialog, returnValue);
}

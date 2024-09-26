using Microsoft.AspNetCore.Components;

namespace Iceshrimp.Backend.Components.Helpers;

/// <summary>
/// Overrides to allow for asynchronous actions to be performed in fully SSR pages before the page gets rendered
/// </summary>
public class AsyncComponentBase : ComponentBase
{
	private bool _initialized;

	public override Task SetParametersAsync(ParameterView parameters)
	{
		parameters.SetParameterProperties(this);
		if (_initialized) return CallOnParametersSetAsync();
		_initialized = true;

		return RunInitAndSetParametersAsync();
	}

	private async Task RunInitAndSetParametersAsync()
	{
		OnInitialized();
		await OnInitializedAsync();
		await CallOnParametersSetAsync();
	}

	private async Task CallOnParametersSetAsync()
	{
		OnParametersSet();
		await OnParametersSetAsync();
		StateHasChanged();
	}
}
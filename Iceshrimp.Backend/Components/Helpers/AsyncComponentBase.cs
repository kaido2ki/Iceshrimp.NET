using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Extensions;
using Microsoft.AspNetCore.Components;

namespace Iceshrimp.Backend.Components.Helpers;

/// <summary>
/// Overrides to allow for asynchronous actions to be performed in fully SSR pages before the page gets rendered
/// </summary>
public class AsyncComponentBase : ComponentBase
{
	[CascadingParameter] public required HttpContext       Context    { get; set; }
	[Inject]             public required DatabaseContext   Database   { get; set; }
	[Inject]             public required NavigationManager Navigation { get; set; }

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
		await RunMethodHandler();
		StateHasChanged();
	}

	protected virtual Task OnPost() => Task.CompletedTask;
	protected virtual Task OnGet()  => Task.CompletedTask;

	private async Task RunMethodHandler()
	{
		if (string.Equals(Context.Request.Method, "GET", StringComparison.InvariantCultureIgnoreCase))
			await OnGet();
		else if (string.Equals(Context.Request.Method, "POST", StringComparison.InvariantCultureIgnoreCase))
			await OnPost();
	}

	protected void RedirectToLogin() => Redirect($"/login?rd={Context.Request.Path.ToString().UrlEncode()}");

	protected void Redirect(string target, bool permanent = false)
	{
		if (permanent)
		{
			Context.Response.OnStarting(() =>
			{
				Context.Response.StatusCode = 301;
				return Task.CompletedTask;
			});
		}

		Navigation.NavigateTo(target);
	}

	protected void ReloadPage() => Navigation.Refresh(true);
}
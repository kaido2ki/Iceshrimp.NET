using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Pages;

public class IndexModel(MetaService meta, IOptionsSnapshot<Config.InstanceSection> config) : PageModel
{
	public string? ContactEmail;
	public string  InstanceDescription = null!;
	public string  InstanceName        = null!;

	public async Task<IActionResult> OnGet()
	{
		if (Request.Cookies.ContainsKey("session") || Request.Cookies.ContainsKey("sessions"))
			return Partial("Shared/FrontendSPA");

		if (config.Value.RedirectIndexTo is { } dest)
			return Redirect(dest);

		var (instanceName, instanceDescription, contactEmail) =
			await meta.GetMany(MetaEntity.InstanceName, MetaEntity.InstanceDescription, MetaEntity.AdminContactEmail);

		InstanceName = instanceName ?? "Iceshrimp.NET";
		InstanceDescription =
			instanceDescription ?? "This Iceshrimp.NET instance does not appear to have a description";
		ContactEmail = contactEmail;

		return Page();
	}
}
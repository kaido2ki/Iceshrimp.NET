using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Iceshrimp.Backend.Pages;

public class IndexModel(MetaService meta) : PageModel
{
	public string? ContactEmail;
	public string  InstanceDescription = null!;
	public string  InstanceName        = null!;

	public async Task<IActionResult> OnGet()
	{
		if (Request.Cookies.ContainsKey("session"))
			return Partial("Shared/FrontendSPA");

		var (instanceName, instanceDescription, contactEmail) =
			await meta.GetMany(MetaEntity.InstanceName, MetaEntity.InstanceDescription, MetaEntity.AdminContactEmail);

		InstanceName = instanceName ?? "Iceshrimp.NET";
		InstanceDescription =
			instanceDescription ?? "This Iceshrimp.NET instance does not appear to have a description";
		ContactEmail = contactEmail;

		return Page();
	}
}
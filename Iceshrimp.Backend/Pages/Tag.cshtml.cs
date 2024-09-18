using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Iceshrimp.Backend.Pages;

public class TagModel : PageModel
{
	public string Tag = null!;

	public IActionResult OnGet(string tag)
	{
		if (Request.Cookies.ContainsKey("session") || Request.Cookies.ContainsKey("sessions"))
			return Partial("Shared/FrontendSPA");

		Tag = tag;
		return Page();
	}
}
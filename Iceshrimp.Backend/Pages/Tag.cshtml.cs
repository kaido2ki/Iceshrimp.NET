using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Iceshrimp.Backend.Pages;

public class TagModel : PageModel
{
	public string Tag = null!;
	public void OnGet(string tag)
	{
		Tag = tag;
	}
}
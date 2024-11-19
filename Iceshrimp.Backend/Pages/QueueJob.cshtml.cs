using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Pages;

public class QueueJobModel(DatabaseContext db, MetaService meta) : PageModel
{
	private static Dictionary<string, string> _lookup = new()
	{
		["inbox"]       = "body",
		["deliver"]     = "payload",
		["pre-deliver"] = "serializedActivity"
	};

	public Job                        Job          = null!;
	public string                     InstanceName = "Iceshrimp.NET";
	public Dictionary<string, string> Lookup => _lookup;

	public async Task<IActionResult> OnGet([FromRoute] Guid id)
	{
		if (!Request.Cookies.TryGetValue("admin_session", out var cookie))
			return Redirect("/login");
		if (!await db.Sessions.AnyAsync(p => p.Token == cookie && p.Active && p.User.IsAdmin))
			return Redirect("/login");

		Request.HttpContext.HideFooter();
		InstanceName = await meta.GetAsync(MetaEntity.InstanceName) ?? InstanceName;

		Job = await db.Jobs.FirstOrDefaultAsync(p => p.Id == id) ??
		      throw GracefulException.NotFound($"Job {id} not found");
		return Page();
	}
}
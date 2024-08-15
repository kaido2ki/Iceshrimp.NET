using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Iceshrimp.Backend.Pages.Shared;

public class ErrorPageModel(ErrorResponse error) : PageModel
{
	public ErrorResponse Error => error;
}
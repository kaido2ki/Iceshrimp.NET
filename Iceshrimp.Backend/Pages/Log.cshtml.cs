using Iceshrimp.Backend.Core.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Iceshrimp.Backend.Pages;

public class LogModel(LogService logService) : PageModel
{
	public IEnumerable<LogEntry> Messages = logService.Logs.Take(100);

	public void OnGet() { }
}
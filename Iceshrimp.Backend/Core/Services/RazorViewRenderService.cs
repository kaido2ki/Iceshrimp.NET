using System.Diagnostics;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Iceshrimp.Backend.Core.Services;

public class RazorViewRenderService(
	IRazorViewEngine razorViewEngine,
	ITempDataProvider tempDataProvider,
	IHttpContextAccessor httpContextAccessor,
	IRazorPageActivator activator
)
{
	private async Task RenderAsync<T>(string path, T model, TextWriter writer) where T : PageModel
	{
		var httpCtx       = httpContextAccessor.HttpContext ?? throw new Exception("HttpContext must not be null");
		var actionContext = new ActionContext(httpCtx, httpCtx.GetRouteData(), new ActionDescriptor());

		var result = razorViewEngine.GetPage("/Pages/", path);
		if (result.Page == null) throw new ArgumentNullException($"The page {path} could not be found.");

		using var diag = new DiagnosticListener("ViewRenderService");
		var view = new RazorView(razorViewEngine, activator, [], result.Page, HtmlEncoder.Default, diag);
		var viewData = new ViewDataDictionary<T>(new EmptyModelMetadataProvider(), []) { Model = model };
		var tempData = new TempDataDictionary(httpContextAccessor.HttpContext, tempDataProvider);
		var viewContext = new ViewContext(actionContext, view, viewData, tempData, writer, new HtmlHelperOptions());

		var page = (Page)result.Page;
		page.PageContext = new PageContext(actionContext) { ViewData = viewContext.ViewData };
		page.ViewContext = viewContext;
		activator.Activate(page, viewContext);
		await page.ExecuteAsync();
	}

	public async Task<string> RenderToStringAsync<T>(string pageName, T model) where T : PageModel
	{
		await using var sw = new StringWriter();
		await RenderAsync(pageName, model, sw);
		return sw.ToString();
	}

	public async Task RenderToStreamAsync<T>(string pageName, T model, Stream stream) where T : PageModel
	{
		await using var sw = new StreamWriter(stream);
		await RenderAsync(pageName, model, sw);
	}
}
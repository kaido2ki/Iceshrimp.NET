using Iceshrimp.Backend.Core.Middleware;

namespace Iceshrimp.Backend.Core.Helpers;

public static class AppExtensions {
	public static WebApplication UseCustomMiddleware(this WebApplication app) {
		app.UseMiddleware<RequestBufferingMiddleware>();

		return app;
	}
}
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Middleware;

public class AuthenticationMiddleware(DatabaseContext db) : IMiddleware {
	public async Task InvokeAsync(HttpContext ctx, RequestDelegate next) {
		var endpoint  = ctx.Features.Get<IEndpointFeature>()?.Endpoint;
		var attribute = endpoint?.Metadata.GetMetadata<AuthenticateAttribute>();

		if (attribute != null) {
			var request = ctx.Request;
			var header  = request.Headers.Authorization.ToString();
			if (!header.ToLowerInvariant().StartsWith("bearer ")) {
				await next(ctx);
				return;
			}

			var token   = header[7..];
			var session = await db.Sessions.Include(p => p.User).FirstOrDefaultAsync(p => p.Token == token && p.Active);
			if (session == null) {
				await next(ctx);
				return;
			}

			ctx.SetSession(session);
		}

		await next(ctx);
	}
}

public class AuthenticateAttribute : Attribute;

public static partial class HttpContextExtensions {
	private const string Key = "session";

	internal static void SetSession(this HttpContext ctx, Session session) {
		ctx.Items.Add(Key, session);
	}

	public static Session? GetSession(this HttpContext ctx) {
		ctx.Items.TryGetValue(Key, out var session);
		return session as Session;
	}

	public static User? GetUser(this HttpContext ctx) {
		ctx.Items.TryGetValue(Key, out var session);
		return (session as Session)?.User;
	}
}
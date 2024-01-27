using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Middleware;

public class AuthenticationMiddleware(
	[SuppressMessage("ReSharper", "SuggestBaseTypeForParameterInConstructor")]
	DatabaseContext db
) : IMiddleware {
	public async Task InvokeAsync(HttpContext ctx, RequestDelegate next) {
		var endpoint  = ctx.Features.Get<IEndpointFeature>()?.Endpoint;
		var attribute = endpoint?.Metadata.GetMetadata<AuthenticationAttribute>();

		if (attribute != null) {
			var request = ctx.Request;
			var header  = request.Headers.Authorization.ToString();
			if (!header.ToLowerInvariant().StartsWith("bearer ")) {
				if (attribute.Required)
					throw GracefulException.Unauthorized("Missing bearer token in authorization header");
				await next(ctx);
				return;
			}

			var token   = header[7..];
			var session = await db.Sessions.Include(p => p.User).FirstOrDefaultAsync(p => p.Token == token);
			if (session == null) {
				if (attribute.Required)
					throw GracefulException.Forbidden("Bearer token is invalid");
				await next(ctx);
				return;
			}
			ctx.SetSession(session);
		}

		await next(ctx);
	}
}

public class AuthenticationAttribute(bool required = true) : Attribute {
	public bool Required { get; } = required;
}

public static class HttpContextExtensions {
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
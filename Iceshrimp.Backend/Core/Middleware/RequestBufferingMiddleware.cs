using Iceshrimp.Backend.Core.Extensions;
using JetBrains.Annotations;

namespace Iceshrimp.Backend.Core.Middleware;

[UsedImplicitly]
public class RequestBufferingMiddleware(RequestDelegate next) : ConditionalMiddleware<EnableRequestBufferingAttribute>
{
	[UsedImplicitly]
	public async Task InvokeAsync(HttpContext ctx)
	{
		var attr = GetAttributeOrFail(ctx);
		ctx.Request.EnableBuffering(attr.MaxLength);
		await next(ctx);
	}
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class EnableRequestBufferingAttribute(long maxLength) : Attribute
{
	internal readonly long MaxLength = maxLength;
}
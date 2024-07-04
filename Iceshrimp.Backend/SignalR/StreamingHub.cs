using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Iceshrimp.Shared.Schemas.SignalR;
using Microsoft.AspNetCore.SignalR;

namespace Iceshrimp.Backend.SignalR;

[Microsoft.AspNetCore.Authorization.Authorize(Policy = "HubAuthorization")]
public class StreamingHub(StreamingService streamingService) : Hub<IStreamingHubClient>, IStreamingHubServer
{
	public Task Subscribe(StreamingTimeline timeline)
	{
		var userId = Context.UserIdentifier ?? throw new Exception("UserIdentifier must not be null at this stage");
		return streamingService.Subscribe(userId, Context.ConnectionId, timeline);
	}

	public Task Unsubscribe(StreamingTimeline timeline)
	{
		var userId = Context.UserIdentifier ?? throw new Exception("UserIdentifier must not be null at this stage");
		return streamingService.Unsubscribe(userId, Context.ConnectionId, timeline);
	}

	public override async Task OnConnectedAsync()
	{
		await base.OnConnectedAsync();
		var ctx    = Context.GetHttpContext() ?? throw new Exception("HttpContext must not be null at this stage");
		var user   = ctx.GetUserOrFail();
		var userId = Context.UserIdentifier ?? throw new Exception("UserIdentifier must not be null at this stage");
		streamingService.Connect(userId, user, Context.ConnectionId);
	}

	public override async Task OnDisconnectedAsync(Exception? exception)
	{
		var userId = Context.UserIdentifier ?? throw new Exception("UserIdentifier must not be null at this stage");
		streamingService.Disconnect(userId, Context.ConnectionId);
		await base.OnDisconnectedAsync(exception);
	}
}
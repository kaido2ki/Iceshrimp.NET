using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Iceshrimp.Backend.Controllers.Mastodon.Streaming.Channels;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Events;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Services;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Mastodon.Streaming;

[MustDisposeResource]
public sealed class WebSocketConnection(
	WebSocket socket,
	OauthToken token,
	EventService eventSvc,
	IServiceScopeFactory scopeFactory,
	CancellationToken ct
) : IDisposable
{
	private readonly WriteLockingList<string> _blockedBy     = [];
	private readonly WriteLockingList<string> _blocking      = [];
	private readonly SemaphoreSlim            _lock          = new(1);
	private readonly WriteLockingList<string> _muting        = [];
	public readonly  List<IChannel>           Channels       = [];
	public readonly  EventService             EventService   = eventSvc;
	public readonly  WriteLockingList<Filter> Filters        = [];
	public readonly  WriteLockingList<string> Following      = [];
	public readonly  IServiceScope            Scope          = scopeFactory.CreateScope();
	public readonly  IServiceScopeFactory     ScopeFactory   = scopeFactory;
	public readonly  OauthToken               Token          = token;
	public           List<string>             HiddenFromHome = [];

	public void Dispose()
	{
		foreach (var channel in Channels)
			channel.Dispose();

		EventService.UserBlocked        -= OnUserUnblock;
		EventService.UserUnblocked      -= OnUserBlock;
		EventService.UserMuted          -= OnUserMute;
		EventService.UserUnmuted        -= OnUserUnmute;
		EventService.UserFollowed       -= OnUserFollow;
		EventService.UserUnfollowed     -= OnUserUnfollow;
		EventService.FilterAdded        -= OnFilterAdded;
		EventService.FilterRemoved      -= OnFilterRemoved;
		EventService.FilterUpdated      -= OnFilterUpdated;
		EventService.ListMembersUpdated -= OnListMembersUpdated;

		Scope.Dispose();
	}

	public void InitializeStreamingWorker()
	{
		Channels.Add(new ListChannel(this));
		Channels.Add(new DirectChannel(this));
		Channels.Add(new UserChannel(this, true));
		Channels.Add(new UserChannel(this, false));
		Channels.Add(new HashtagChannel(this, true));
		Channels.Add(new HashtagChannel(this, false));
		Channels.Add(new PublicChannel(this, "public", true, true, false));
		Channels.Add(new PublicChannel(this, "public:media", true, true, true));
		Channels.Add(new PublicChannel(this, "public:allow_local_only", true, true, false));
		Channels.Add(new PublicChannel(this, "public:allow_local_only:media", true, true, true));
		Channels.Add(new PublicChannel(this, "public:local", true, false, false));
		Channels.Add(new PublicChannel(this, "public:local:media", true, false, true));
		Channels.Add(new PublicChannel(this, "public:remote", false, true, false));
		Channels.Add(new PublicChannel(this, "public:remote:media", false, true, true));

		EventService.UserBlocked        += OnUserUnblock;
		EventService.UserUnblocked      += OnUserBlock;
		EventService.UserMuted          += OnUserMute;
		EventService.UserUnmuted        += OnUserUnmute;
		EventService.UserFollowed       += OnUserFollow;
		EventService.UserUnfollowed     += OnUserUnfollow;
		EventService.FilterAdded        += OnFilterAdded;
		EventService.FilterRemoved      += OnFilterRemoved;
		EventService.FilterUpdated      += OnFilterUpdated;
		EventService.ListMembersUpdated += OnListMembersUpdated;

		_ = InitializeRelationships();
	}

	private async Task InitializeRelationships()
	{
		await using var db = Scope.ServiceProvider.GetRequiredService<DatabaseContext>();
		Following.AddRange(await db.Followings.Where(p => p.Follower == Token.User)
		                           .Select(p => p.FolloweeId)
		                           .ToListAsync());
		_blocking.AddRange(await db.Blockings.Where(p => p.Blocker == Token.User)
		                           .Select(p => p.BlockeeId)
		                           .ToListAsync());
		_blockedBy.AddRange(await db.Blockings.Where(p => p.Blockee == Token.User)
		                            .Select(p => p.BlockerId)
		                            .ToListAsync());
		_muting.AddRange(await db.Mutings.Where(p => p.Muter == Token.User)
		                         .Select(p => p.MuteeId)
		                         .ToListAsync());

		Filters.AddRange(await db.Filters.Where(p => p.User == Token.User)
		                         .Select(p => new Filter
		                         {
			                         Name     = p.Name,
			                         Action   = p.Action,
			                         Contexts = p.Contexts,
			                         Expiry   = p.Expiry,
			                         Id       = p.Id,
			                         Keywords = p.Keywords
		                         })
		                         .AsNoTracking()
		                         .ToListAsync());

		HiddenFromHome = await db.UserListMembers
		                         .Where(p => p.UserList.UserId == Token.User.Id && p.UserList.HideFromHomeTl)
		                         .Select(p => p.UserId)
		                         .Distinct()
		                         .ToListAsync();
	}

	public async Task HandleSocketMessageAsync(string payload)
	{
		StreamingRequestMessage? message = null;
		try
		{
			message = JsonSerializer.Deserialize<StreamingRequestMessage>(payload);
		}
		catch
		{
			// ignored
		}

		if (message == null)
		{
			await CloseAsync(WebSocketCloseStatus.InvalidPayloadData);
			return;
		}

		await HandleSocketMessageAsync(message);
	}

	public async Task HandleSocketMessageAsync(StreamingRequestMessage message)
	{
		switch (message.Type)
		{
			case "subscribe":
			{
				var channel =
					Channels.FirstOrDefault(p => p.Name == message.Stream && (!p.IsSubscribed || p.IsAggregate));
				if (channel == null) return;
				if (channel.Scopes.Except(MastodonOauthHelpers.ExpandScopes(Token.Scopes)).Any())
					await CloseAsync(WebSocketCloseStatus.PolicyViolation);
				else
					await channel.Subscribe(message);
				break;
			}
			case "unsubscribe":
			{
				var channel =
					Channels.FirstOrDefault(p => p.Name == message.Stream && (p.IsSubscribed || p.IsAggregate));
				if (channel != null) await channel.Unsubscribe(message);
				break;
			}
			default:
			{
				await CloseAsync(WebSocketCloseStatus.InvalidPayloadData);
				return;
			}
		}
	}

	public async Task SendMessageAsync(string message)
	{
		await _lock.WaitAsync(ct);
		try
		{
			var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
			await socket.SendAsync(buffer, WebSocketMessageType.Text, true, ct);
		}
		finally
		{
			_lock.Release();
		}
	}

	private void OnUserBlock(object? _, UserInteraction interaction)
	{
		try
		{
			if (interaction.Actor.Id == Token.User.Id)
				_blocking.Add(interaction.Object.Id);

			if (interaction.Object.Id == Token.User.Id)
				_blockedBy.Add(interaction.Actor.Id);
		}
		catch (Exception e)
		{
			var logger = Scope.ServiceProvider.GetRequiredService<Logger<WebSocketConnection>>();
			logger.LogError("Event handler OnUserBlock threw exception: {e}", e);
		}
	}

	private void OnUserUnblock(object? _, UserInteraction interaction)
	{
		try
		{
			if (interaction.Actor.Id == Token.User.Id)
				_blocking.Remove(interaction.Object.Id);

			if (interaction.Object.Id == Token.User.Id)
				_blockedBy.Remove(interaction.Actor.Id);
		}
		catch (Exception e)
		{
			var logger = Scope.ServiceProvider.GetRequiredService<Logger<WebSocketConnection>>();
			logger.LogError("Event handler OnUserUnblock threw exception: {e}", e);
		}
	}

	private void OnUserMute(object? _, UserInteraction interaction)
	{
		try
		{
			if (interaction.Actor.Id == Token.User.Id)
				_muting.Add(interaction.Object.Id);
		}
		catch (Exception e)
		{
			var logger = Scope.ServiceProvider.GetRequiredService<Logger<WebSocketConnection>>();
			logger.LogError("Event handler OnUserMute threw exception: {e}", e);
		}
	}

	private void OnUserUnmute(object? _, UserInteraction interaction)
	{
		try
		{
			if (interaction.Actor.Id == Token.User.Id)
				_muting.Remove(interaction.Object.Id);
		}
		catch (Exception e)
		{
			var logger = Scope.ServiceProvider.GetRequiredService<Logger<WebSocketConnection>>();
			logger.LogError("Event handler OnUserUnmute threw exception: {e}", e);
		}
	}

	private void OnUserFollow(object? _, UserInteraction interaction)
	{
		try
		{
			if (interaction.Actor.Id == Token.User.Id)
				Following.Add(interaction.Object.Id);
		}
		catch (Exception e)
		{
			var logger = Scope.ServiceProvider.GetRequiredService<Logger<WebSocketConnection>>();
			logger.LogError("Event handler OnUserFollow threw exception: {e}", e);
		}
	}

	private void OnUserUnfollow(object? _, UserInteraction interaction)
	{
		try
		{
			if (interaction.Actor.Id == Token.User.Id)
				Following.Remove(interaction.Object.Id);
		}
		catch (Exception e)
		{
			var logger = Scope.ServiceProvider.GetRequiredService<Logger<WebSocketConnection>>();
			logger.LogError("Event handler OnUserUnfollow threw exception: {e}", e);
		}
	}

	private void OnFilterAdded(object? _, Filter filter)
	{
		try
		{
			if (filter.User.Id != Token.User.Id) return;
			Filters.Add(filter.Clone(Token.User));
		}
		catch (Exception e)
		{
			var logger = Scope.ServiceProvider.GetRequiredService<Logger<WebSocketConnection>>();
			logger.LogError("Event handler OnFilterAdded threw exception: {e}", e);
		}
	}

	private void OnFilterRemoved(object? _, Filter filter)
	{
		try
		{
			if (filter.User.Id != Token.User.Id) return;
			var match = Filters.FirstOrDefault(p => p.Id == filter.Id);
			if (match != null) Filters.Remove(match);
		}
		catch (Exception e)
		{
			var logger = Scope.ServiceProvider.GetRequiredService<Logger<WebSocketConnection>>();
			logger.LogError("Event handler OnFilterRemoved threw exception: {e}", e);
		}
	}

	private void OnFilterUpdated(object? _, Filter filter)
	{
		try
		{
			if (filter.User.Id != Token.User.Id) return;
			var match = Filters.FirstOrDefault(p => p.Id == filter.Id);
			if (match == null) Filters.Add(filter.Clone(Token.User));
			else
			{
				match.Contexts = filter.Contexts;
				match.Action   = filter.Action;
				match.Keywords = filter.Keywords;
				match.Name     = filter.Name;
			}
		}
		catch (Exception e)
		{
			var logger = Scope.ServiceProvider.GetRequiredService<Logger<WebSocketConnection>>();
			logger.LogError("Event handler OnFilterUpdated threw exception: {e}", e);
		}
	}

	private async void OnListMembersUpdated(object? _, UserList list)
	{
		try
		{
			if (list.UserId != Token.User.Id) return;
			if (!list.HideFromHomeTl) return;

			await using var scope = ScopeFactory.CreateAsyncScope();

			var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
			HiddenFromHome = await db.UserListMembers
			                         .Where(p => p.UserList.UserId == Token.User.Id && p.UserList.HideFromHomeTl)
			                         .Select(p => p.UserId)
			                         .ToListAsync();
		}
		catch (Exception e)
		{
			var logger = Scope.ServiceProvider.GetRequiredService<Logger<WebSocketConnection>>();
			logger.LogError("Event handler OnListMembersUpdated threw exception: {e}", e);
		}
	}

	[SuppressMessage("ReSharper", "SuggestBaseTypeForParameter")]
	public bool IsFiltered(User user) =>
		_blocking.Contains(user.Id) || _blockedBy.Contains(user.Id) || _muting.Contains(user.Id);

	private bool IsFilteredMentions(IReadOnlyCollection<string> userIds) =>
		_blocking.Intersects(userIds) || _muting.Intersects(userIds);

	public bool IsFiltered(Note note) => IsFiltered(note.User) ||
	                                     IsFilteredMentions(note.Mentions) ||
	                                     (note.Renote?.User != null &&
	                                      (IsFiltered(note.Renote.User) ||
	                                       IsFilteredMentions(note.Renote.Mentions))) ||
	                                     (note.Renote?.Renote?.User != null &&
	                                      (IsFiltered(note.Renote.Renote.User) ||
	                                       IsFilteredMentions(note.Renote.Renote.Mentions)));

	public async Task<bool> IsMutedThread(Note note, AsyncServiceScope scope)
	{
		if (note.Reply == null) return false;
		if (note.User.Id == Token.UserId) return false;
		var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
		return await db.NoteThreadMutings.AnyAsync(p => p.UserId == Token.UserId && p.ThreadId == note.ThreadId);
	}

	public async Task CloseAsync(WebSocketCloseStatus status)
	{
		Dispose();
		await socket.CloseAsync(status, null, ct);
	}
}

public interface IChannel
{
	public string       Name         { get; }
	public List<string> Scopes       { get; }
	public bool         IsSubscribed { get; }
	public bool         IsAggregate  { get; }
	public Task         Subscribe(StreamingRequestMessage message);
	public Task         Unsubscribe(StreamingRequestMessage message);
	public void         Dispose();
}
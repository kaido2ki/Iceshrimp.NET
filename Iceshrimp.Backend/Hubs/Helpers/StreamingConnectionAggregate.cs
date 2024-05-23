using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Controllers.Renderers;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Events;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Services;
using Iceshrimp.Shared.HubSchemas;
using Iceshrimp.Shared.Schemas;
using JetBrains.Annotations;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Hubs.Helpers;

[MustDisposeResource]
public sealed class StreamingConnectionAggregate : IDisposable
{
	private readonly User                     _user;
	private readonly string                   _userId;
	private readonly WriteLockingList<string> _connectionIds = [];

	private readonly IHubContext<StreamingHub, IStreamingHubClient> _hub;

	private readonly EventService         _eventService;
	private readonly IServiceScope        _scope;
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly StreamingService     _streamingService;
	private readonly ILogger              _logger;

	private readonly WriteLockingList<string> _following      = [];
	private readonly WriteLockingList<string> _muting         = [];
	private readonly WriteLockingList<string> _blocking       = [];
	private readonly WriteLockingList<string> _blockedBy      = [];
	private          List<string>             _hiddenFromHome = [];

	private readonly ConcurrentDictionary<string, WriteLockingList<StreamingTimeline>> _subscriptions = [];

	public bool HasSubscribers => _connectionIds.Count != 0;

	private AsyncServiceScope GetTempScope() => _scopeFactory.CreateAsyncScope();

	#region Initialization

	public StreamingConnectionAggregate(
		string userId,
		User user,
		IHubContext<StreamingHub, IStreamingHubClient> hub,
		EventService eventSvc,
		IServiceScopeFactory scopeFactory, StreamingService streamingService
	)
	{
		if (userId != user.Id)
			throw new Exception("userId doesn't match user.Id");

		_userId           = userId;
		_user             = user;
		_hub              = hub;
		_eventService     = eventSvc;
		_scope            = scopeFactory.CreateScope();
		_scopeFactory     = scopeFactory;
		_streamingService = streamingService;
		_logger           = _scope.ServiceProvider.GetRequiredService<ILogger<StreamingConnectionAggregate>>();

		_ = InitializeAsync();
	}

	private async Task InitializeAsync()
	{
		_eventService.UserBlocked    += OnUserBlock;
		_eventService.UserUnblocked  += OnUserUnblock;
		_eventService.UserMuted      += OnUserMute;
		_eventService.UserUnmuted    += OnUserUnmute;
		_eventService.UserFollowed   += OnUserFollow;
		_eventService.UserUnfollowed += OnUserUnfollow;

		await InitializeRelationships();

		_eventService.Notification      += OnNotification;
		_streamingService.NotePublished += OnNotePublished;
		_streamingService.NoteUpdated   += OnNoteUpdated;
	}

	private async Task InitializeRelationships()
	{
		await using var scope = GetTempScope();
		await using var db    = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
		_following.AddRange(await db.Followings.Where(p => p.Follower == _user)
		                            .Select(p => p.FolloweeId)
		                            .ToListAsync());
		_blocking.AddRange(await db.Blockings.Where(p => p.Blocker == _user)
		                           .Select(p => p.BlockeeId)
		                           .ToListAsync());
		_blockedBy.AddRange(await db.Blockings.Where(p => p.Blockee == _user)
		                            .Select(p => p.BlockerId)
		                            .ToListAsync());
		_muting.AddRange(await db.Mutings.Where(p => p.Muter == _user)
		                         .Select(p => p.MuteeId)
		                         .ToListAsync());

		_hiddenFromHome = await db.UserListMembers
		                          .Where(p => p.UserList.User == _user && p.UserList.HideFromHomeTl)
		                          .Select(p => p.UserId)
		                          .Distinct()
		                          .ToListAsync();
	}

	#endregion

	#region Channel subscription handlers

	public void Subscribe(string connectionId, StreamingTimeline timeline)
	{
		if (!_connectionIds.Contains(connectionId)) return;
		_subscriptions.GetOrAdd(connectionId, []).Add(timeline);
	}

	public void Unsubscribe(string connectionId, StreamingTimeline timeline)
	{
		if (!_connectionIds.Contains(connectionId)) return;
		_subscriptions.TryGetValue(connectionId, out var collection);
		collection?.Remove(timeline);
	}

	#endregion

	private async void OnNotification(object? _, Notification notification)
	{
		try
		{
			if (notification.NotifieeId != _userId) return;
			if (notification.Notifier != null && IsFiltered(notification.Notifier)) return;
			await using var scope = GetTempScope();

			var renderer = scope.ServiceProvider.GetRequiredService<NotificationRenderer>();
			var rendered = await renderer.RenderOne(notification, _user);
			await _hub.Clients.User(_userId).Notification(rendered);
		}
		catch (Exception e)
		{
			_logger.LogError("Event handler OnNotification threw exception: {e}", e);
		}
	}

	private async void OnNotePublished(object? _, (Note note, Func<Task<NoteResponse>> rendered) data)
	{
		try
		{
			var wrapped = IsApplicable(data.note);
			if (wrapped == null) return;
			var recipients = FindRecipients(data.note);
			if (recipients.connectionIds.Count == 0) return;

			var rendered = EnforceRenoteReplyVisibility(await data.rendered(), wrapped);
			await _hub.Clients.Clients(recipients.connectionIds).NotePublished(recipients.timelines, rendered);
		}
		catch (Exception e)
		{
			_logger.LogError("Event handler {name} threw exception: {e}", nameof(OnNotePublished), e);
		}
	}

	private async void OnNoteUpdated(object? _, (Note note, Func<Task<NoteResponse>> rendered) data)
	{
		try
		{
			var wrapped = IsApplicable(data.note);
			if (wrapped == null) return;
			var recipients = FindRecipients(data.note);
			if (recipients.connectionIds.Count == 0) return;

			var rendered = EnforceRenoteReplyVisibility(await data.rendered(), wrapped);
			await _hub.Clients.Clients(recipients.connectionIds).NoteUpdated(recipients.timelines, rendered);
		}
		catch (Exception e)
		{
			_logger.LogError("Event handler {name} threw exception: {e}", nameof(OnNoteUpdated), e);
		}
	}

	private static NoteResponse EnforceRenoteReplyVisibility(NoteResponse rendered, NoteWithVisibilities note)
	{
		var renote = note.Renote == null && rendered.Renote != null;
		var reply  = note.Reply == null && rendered.Reply != null;
		if (!renote && !reply) return rendered;

		rendered = (NoteResponse)rendered.Clone();
		if (renote) rendered.Renote = null;
		if (reply) rendered.Reply   = null;
		return rendered;
	}

	private NoteWithVisibilities? IsApplicable(Note note)
	{
		if (_subscriptions.IsEmpty) return null;
		if (!note.IsVisibleFor(_user, _following)) return null;
		if (note.Visibility != Note.NoteVisibility.Public && !IsFollowingOrSelf(note.User)) return null;
		if (IsFiltered(note)) return null;
		if (note.Reply != null && IsFiltered(note.Reply)) return null;
		if (note.Renote != null && IsFiltered(note.Renote)) return null;
		if (note.Renote?.Renote != null && IsFiltered(note.Renote.Renote)) return null;

		var res = EnforceRenoteReplyVisibility(note);
		return res is not { Note.IsPureRenote: true, Renote: null } ? res : null;
	}

	[SuppressMessage("ReSharper", "SuggestBaseTypeForParameter")]
	private bool IsFiltered(Note note) =>
		IsFiltered(note.User) || _blocking.Intersects(note.Mentions) || _muting.Intersects(note.Mentions);

	[SuppressMessage("ReSharper", "SuggestBaseTypeForParameter")]
	private bool IsFiltered(User user) =>
		!_blockedBy.Contains(user.Id) && !_blocking.Contains(user.Id) && !_muting.Contains(user.Id);

	[SuppressMessage("ReSharper", "SuggestBaseTypeForParameter")]
	private bool IsFollowingOrSelf(User user) => user.Id == _userId || _following.Contains(user.Id);

	private NoteWithVisibilities EnforceRenoteReplyVisibility(Note note)
	{
		var wrapped = new NoteWithVisibilities(note);
		if (!wrapped.Renote?.IsVisibleFor(_user, _following) ?? false)
			wrapped.Renote = null;
		if (!wrapped.Reply?.IsVisibleFor(_user, _following) ?? false)
			wrapped.Reply = null;

		return wrapped;
	}

	private class NoteWithVisibilities(Note note)
	{
		public readonly Note  Note   = note;
		public          Note? Reply  = note.Reply;
		public          Note? Renote = note.Renote;
	}

	private (List<string> connectionIds, List<StreamingTimeline> timelines) FindRecipients(Note note)
	{
		List<StreamingTimeline> timelines = [];
		if (note.Visibility == Note.NoteVisibility.Public)
		{
			timelines.Add(StreamingTimeline.Federated);

			if (note.UserHost == null)
				timelines.Add(StreamingTimeline.Local);

			if (IsFollowingOrSelf(note.User) && note.CreatedAt > DateTime.UtcNow - TimeSpan.FromMinutes(5))
				if (!_hiddenFromHome.Contains(note.UserId))
					timelines.Add(StreamingTimeline.Home);
		}
		else if (note.CreatedAt > DateTime.UtcNow - TimeSpan.FromMinutes(5) && !_hiddenFromHome.Contains(note.UserId))
		{
			// We already enumerated _following in IsApplicable()
			timelines.Add(StreamingTimeline.Home);
		}

		var connectionIds = _subscriptions.Where(p => p.Value.Intersects(timelines)).Select(p => p.Key).ToList();

		return (connectionIds, timelines);
	}

	#region Relationship change event handlers

	private void OnUserBlock(object? _, UserInteraction interaction)
	{
		try
		{
			if (interaction.Actor.Id == _userId)
			{
				_blocking.Add(interaction.Object.Id);
				_following.Remove(interaction.Object.Id);
			}
			else if (interaction.Object.Id == _userId)
			{
				_blockedBy.Add(interaction.Actor.Id);
				_following.Remove(interaction.Actor.Id);
			}
		}
		catch (Exception e)
		{
			_logger.LogError("Event handler OnUserBlock threw exception: {e}", e);
		}
	}

	private void OnUserUnblock(object? _, UserInteraction interaction)
	{
		try
		{
			if (interaction.Actor.Id == _userId)
				_blocking.Remove(interaction.Object.Id);

			if (interaction.Object.Id == _userId)
				_blockedBy.Remove(interaction.Actor.Id);
		}
		catch (Exception e)
		{
			_logger.LogError("Event handler OnUserUnblock threw exception: {e}", e);
		}
	}

	private void OnUserMute(object? _, UserInteraction interaction)
	{
		try
		{
			if (interaction.Actor.Id == _userId)
				_muting.Add(interaction.Object.Id);
		}
		catch (Exception e)
		{
			_logger.LogError("Event handler OnUserMute threw exception: {e}", e);
		}
	}

	private void OnUserUnmute(object? _, UserInteraction interaction)
	{
		try
		{
			if (interaction.Actor.Id == _userId)
				_muting.Remove(interaction.Object.Id);
		}
		catch (Exception e)
		{
			_logger.LogError("Event handler OnUserUnmute threw exception: {e}", e);
		}
	}

	private void OnUserFollow(object? _, UserInteraction interaction)
	{
		try
		{
			if (interaction.Actor.Id == _userId)
				_following.Add(interaction.Object.Id);
		}
		catch (Exception e)
		{
			_logger.LogError("Event handler OnUserFollow threw exception: {e}", e);
		}
	}

	private void OnUserUnfollow(object? _, UserInteraction interaction)
	{
		try
		{
			if (interaction.Actor.Id == _userId)
				_following.Remove(interaction.Object.Id);
		}
		catch (Exception e)
		{
			_logger.LogError("Event handler OnUserUnfollow threw exception: {e}", e);
		}
	}

	#endregion

	#region Connection status methods

	public void Connect(string connectionId)
	{
		_connectionIds.Add(connectionId);
	}

	public void Disconnect(string connectionId)
	{
		_connectionIds.Remove(connectionId);
		_subscriptions.TryRemove(connectionId, out _);
	}

	private void DisconnectAll()
	{
		_connectionIds.Clear();
		_subscriptions.Clear();
	}

	#endregion

	#region Destruction

	public void Dispose()
	{
		DisconnectAll();
		_streamingService.NotePublished -= OnNotePublished;
		_streamingService.NoteUpdated   -= OnNoteUpdated;
		_eventService.Notification      -= OnNotification;
		_eventService.UserBlocked       -= OnUserBlock;
		_eventService.UserUnblocked     -= OnUserUnblock;
		_eventService.UserMuted         -= OnUserMute;
		_eventService.UserUnmuted       -= OnUserUnmute;
		_eventService.UserFollowed      -= OnUserFollow;
		_eventService.UserUnfollowed    -= OnUserUnfollow;
		_scope.Dispose();
	}

	#endregion
}
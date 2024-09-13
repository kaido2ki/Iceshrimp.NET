using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Controllers.Mastodon.Renderers;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Mastodon;

[MastodonApiController]
[Route("/api/v1/conversations")]
[Authenticate]
[EnableRateLimiting("sliding")]
[EnableCors("mastodon")]
[Produces(MediaTypeNames.Application.Json)]
public class ConversationsController(
	DatabaseContext db,
	NoteRenderer noteRenderer,
	UserRenderer userRenderer
) : ControllerBase
{
	[HttpGet]
	[Authorize("read:statuses")]
	[LinkPagination(20, 40)]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<IEnumerable<ConversationEntity>> GetConversations(MastodonPaginationQuery pq)
	{
		var user = HttpContext.GetUserOrFail();

		//TODO: rewrite using .DistinctBy when https://github.com/npgsql/efcore.pg/issues/894 is implemented

		var conversations = await db.Conversations(user)
		                            .IncludeCommonProperties()
		                            .FilterHiddenConversations(user, db)
		                            .FilterMutedThreads(user, db)
		                            .Paginate(p => p.ThreadIdOrId, pq, ControllerContext)
		                            .Select(p => new Conversation
		                            {
			                            Id       = p.ThreadIdOrId,
			                            LastNote = p,
			                            UserIds  = p.VisibleUserIds,
			                            Unread = db.Notifications.Any(n => n.Note == p &&
			                                                               n.Notifiee == user &&
			                                                               !n.IsRead &&
			                                                               (n.Type ==
			                                                                Notification.NotificationType.Reply ||
			                                                                n.Type ==
			                                                                Notification.NotificationType.Mention))
		                            })
		                            .ToListAsync();

		var userIds = conversations.SelectMany(i => i.UserIds)
		                           .Concat(conversations.Select(p => p.LastNote.UserId))
		                           .Distinct();

		var users = await db.Users.IncludeCommonProperties()
		                    .Where(p => userIds.Contains(p.Id))
		                    .ToListAsync();

		var accounts = await userRenderer.RenderManyAsync(users).ToListAsync();

		var notes = await noteRenderer.RenderManyAsync(conversations.Select(p => p.LastNote), user, accounts: accounts);

		return conversations.Select(p => new ConversationEntity
		{
			Id         = p.Id,
			Unread     = p.Unread,
			LastStatus = notes.First(n => n.Id == p.LastNote.Id),
			Accounts = accounts.Where(a => p.UserIds.Any(u => u == a.Id))
			                   .DefaultIfEmpty(accounts.First(a => a.Id == user.Id))
			                   .ToList()
		});
	}

	[HttpDelete("{id}")]
	[Authorize("write:conversations")]
	[ProducesErrors(HttpStatusCode.NotImplemented)]
	public IActionResult RemoveConversation(string id) => throw new GracefulException(HttpStatusCode.NotImplemented,
		"Iceshrimp.NET does not support this endpoint due to database schema differences to Mastodon");

	[HttpPost("{id}/read")]
	[Authorize("write:conversations")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<ConversationEntity> MarkRead(string id)
	{
		var user = HttpContext.GetUserOrFail();
		var conversation = await db.Conversations(user)
		                           .IncludeCommonProperties()
		                           .Where(p => p.ThreadIdOrId == id)
		                           .Select(p => new Conversation
		                           {
			                           Id       = p.ThreadIdOrId,
			                           LastNote = p,
			                           UserIds  = p.VisibleUserIds,
			                           Unread = db.Notifications.Any(n => n.Note == p &&
			                                                              n.Notifiee == user &&
			                                                              !n.IsRead &&
			                                                              (n.Type ==
			                                                               Notification.NotificationType.Reply ||
			                                                               n.Type ==
			                                                               Notification.NotificationType.Mention))
		                           })
		                           .FirstOrDefaultAsync() ??
		                   throw GracefulException.RecordNotFound();

		if (conversation.Unread)
		{
			await db.Notifications.Where(n => n.Note == conversation.LastNote &&
			                                  n.Notifiee == user &&
			                                  !n.IsRead &&
			                                  (n.Type ==
			                                   Notification.NotificationType.Reply ||
			                                   n.Type ==
			                                   Notification.NotificationType.Mention))
			        .ExecuteUpdateAsync(p => p.SetProperty(n => n.IsRead, true));
			conversation.Unread = false;
		}

		var userIds = conversation.UserIds.Append(conversation.LastNote.UserId).Distinct();

		var users = await db.Users.IncludeCommonProperties()
		                    .Where(p => userIds.Contains(p.Id))
		                    .ToListAsync();

		var accounts = await userRenderer.RenderManyAsync(users).ToListAsync();

		var noteRendererDto = new NoteRenderer.NoteRendererDto { Accounts = accounts };

		return new ConversationEntity
		{
			Id         = conversation.Id,
			Unread     = conversation.Unread,
			LastStatus = await noteRenderer.RenderAsync(conversation.LastNote, user, data: noteRendererDto),
			Accounts   = accounts
		};
	}

	private class Conversation
	{
		public required Note         LastNote;
		public required bool         Unread;
		public required List<string> UserIds;
		public required string       Id { get; init; }
	}
}
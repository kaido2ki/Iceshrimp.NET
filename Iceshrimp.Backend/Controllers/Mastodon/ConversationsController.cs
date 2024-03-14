using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Attributes;
using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Controllers.Mastodon.Renderers;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
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
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ConversationEntity>))]
	public async Task<IActionResult> GetConversations(MastodonPaginationQuery pq)
	{
		var user = HttpContext.GetUserOrFail();

		//TODO: rewrite using .DistinctBy when https://github.com/npgsql/efcore.pg/issues/894 is implemented

		var conversations = await db.Conversations(user)
		                            .IncludeCommonProperties()
		                            .FilterMutedConversations(user, db)
		                            .FilterBlockedConversations(user, db)
		                            .Paginate(p => p.ThreadId ?? p.Id, pq, ControllerContext)
		                            .Select(p => new Conversation
		                            {
			                            Id       = p.ThreadId ?? p.Id,
			                            LastNote = p,
			                            Users = db.Users.IncludeCommonProperties()
			                                      .Where(u => p.VisibleUserIds.Contains(u.Id) || u.Id == p.UserId)
			                                      .ToList(),
			                            Unread = db.Notifications.Any(n => n.Note == p &&
			                                                               n.Notifiee == user &&
			                                                               !n.IsRead &&
			                                                               (n.Type ==
			                                                                Notification.NotificationType.Reply ||
			                                                                n.Type ==
			                                                                Notification.NotificationType.Mention))
		                            })
		                            .ToListAsync();

		var accounts = (await userRenderer.RenderManyAsync(conversations.SelectMany(p => p.Users)))
		               .DistinctBy(p => p.Id)
		               .ToList();

		var notes = await noteRenderer.RenderManyAsync(conversations.Select(p => p.LastNote), user, accounts);

		var res = conversations.Select(p => new ConversationEntity
		{
			Id         = p.Id,
			Unread     = p.Unread,
			LastStatus = notes.First(n => n.Id == p.LastNote.Id),
			Accounts = accounts.Where(a => p.Users.Any(u => u.Id == a.Id))
			                   .DefaultIfEmpty(accounts.First(a => a.Id == user.Id))
			                   .ToList()
		});

		return Ok(res);
	}

	[HttpDelete("{id}")]
	[Authorize("write:conversations")]
	[ProducesResponseType(StatusCodes.Status501NotImplemented, Type = typeof(MastodonErrorResponse))]
	public IActionResult RemoveConversation(string id) => throw new GracefulException(HttpStatusCode.NotImplemented,
		"Iceshrimp.NET does not support this endpoint due to database schema differences to Mastodon");

	[HttpPost("{id}/read")]
	[Authorize("write:conversations")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ConversationEntity))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> MarkRead(string id)
	{
		var user = HttpContext.GetUserOrFail();
		var conversation = await db.Conversations(user)
		                           .IncludeCommonProperties()
		                           .Where(p => (p.ThreadId ?? p.Id) == id)
		                           .Select(p => new Conversation
		                           {
			                           Id       = p.ThreadId ?? p.Id,
			                           LastNote = p,
			                           Users = db.Users.IncludeCommonProperties()
			                                     .Where(u => p.VisibleUserIds.Contains(u.Id) || u.Id == p.UserId)
			                                     .ToList(),
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

		var accounts = (await userRenderer.RenderManyAsync(conversation.Users))
		               .DistinctBy(p => p.Id)
		               .ToList();

		var noteRendererDto = new NoteRenderer.NoteRendererDto { Accounts = accounts };

		var res = new ConversationEntity
		{
			Id         = conversation.Id,
			Unread     = conversation.Unread,
			LastStatus = await noteRenderer.RenderAsync(conversation.LastNote, user, noteRendererDto),
			Accounts   = accounts
		};

		return Ok(res);
	}

	private class Conversation
	{
		public required string     Id { get; init; }
		public required Note       LastNote;
		public required List<User> Users;
		public required bool       Unread;
	}
}
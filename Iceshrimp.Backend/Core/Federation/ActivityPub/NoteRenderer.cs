using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Conversion;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Federation.ActivityPub;

public class NoteRenderer(IOptions<Config.InstanceSection> config, MfmConverter mfmConverter, DatabaseContext db)
{
	/// <summary>
	///     This function is meant for compacting a note into the @id form as specified in ActivityStreams
	/// </summary>
	/// <param name="note">Any note</param>
	/// <returns>ASNote with only the Id field populated</returns>
	public ASNote RenderLite(Note note)
	{
		return new ASNote(false) { Id = note.Uri ?? note.GetPublicUri(config.Value) };
	}

	public async Task<ASNote> RenderAsync(Note note, List<Note.MentionedUser>? mentions = null)
	{
		if (note.IsPureRenote)
			throw GracefulException.BadRequest("Refusing to render pure renote as ASNote");

		var id      = note.GetPublicUri(config.Value);
		var userId  = note.User.GetPublicUri(config.Value);
		var replies = new ASOrderedCollection($"{id}/replies");
		var replyId = note.Reply != null
			? new ASObjectBase(note.Reply.Uri ?? note.Reply.GetPublicUri(config.Value))
			: null;

		mentions ??= await db.Users
		                     .Where(p => note.Mentions.Contains(p.Id))
		                     .IncludeCommonProperties()
		                     .Select(p => new Note.MentionedUser
		                     {
			                     Host     = p.Host ?? config.Value.AccountDomain,
			                     Username = p.Username,
			                     Url      = p.UserProfile != null ? p.UserProfile.Url : null,
			                     Uri      = p.Uri ?? p.GetPublicUri(config.Value)
		                     })
		                     .ToListAsync();

		var visible = await db.Users
		                      .Where(p => note.VisibleUserIds.Contains(id) && !note.Mentions.Contains(id))
		                      .IncludeCommonProperties()
		                      .Select(p => new Note.MentionedUser
		                      {
			                      Host     = p.Host ?? config.Value.AccountDomain,
			                      Username = p.Username,
			                      Url      = p.UserProfile != null ? p.UserProfile.Url : null,
			                      Uri      = p.Uri ?? p.GetPublicUri(config.Value)
		                      })
		                      .ToListAsync();

		var emoji = note.Emojis.Count != 0
			? await db.Emojis.Where(p => note.Emojis.Contains(p.Id) && p.Host == null).ToListAsync()
			: [];

		var recipients = mentions.Concat(visible).Select(p => new ASObjectBase(p.Uri)).ToList();

		var to = note.Visibility switch
		{
			Note.NoteVisibility.Public    => [new ASLink($"{Constants.ActivityStreamsNs}#Public")],
			Note.NoteVisibility.Home      => [new ASLink($"{userId}/followers")],
			Note.NoteVisibility.Followers => [new ASLink($"{userId}/followers")],
			Note.NoteVisibility.Specified => recipients,
			_                             => []
		};

		var cc = note.Visibility switch
		{
			Note.NoteVisibility.Public    => [new ASLink($"{userId}/followers"), ..recipients],
			Note.NoteVisibility.Home      => [new ASLink($"{Constants.ActivityStreamsNs}#Public"), ..recipients],
			Note.NoteVisibility.Followers => recipients,
			_                             => []
		};

		var tags = note.Tags.Select(tag => new ASHashtag
		               {
			               Name = $"#{tag}",
			               Href = new ASObjectBase($"https://{config.Value.WebDomain}/tags/{tag}")
		               })
		               .Concat<ASTag>(mentions.Select(mention => new ASMention
		               {
			               Name = $"@{mention.Username}@{mention.Host}",
			               Href = new ASObjectBase(mention.Uri)
		               }))
		               .Concat(emoji.Select(e => new ASEmoji
		               {
			               Id    = e.GetPublicUri(config.Value),
			               Name  = e.Name,
			               Image = new ASImage { Url = new ASLink(e.PublicUrl) }
		               }))
		               .ToList();

		var attachments = note.FileIds.Count > 0
			? await db.DriveFiles
			          .Where(p => note.FileIds.Contains(p.Id) && p.UserHost == null)
			          .Select(p => new ASDocument
			          {
				          Sensitive   = p.IsSensitive,
				          Url         = new ASLink(p.AccessUrl),
				          MediaType   = p.Type,
				          Description = p.Comment
			          })
			          .Cast<ASAttachment>()
			          .ToListAsync()
			: null;

		var quoteUri = note.IsQuote ? note.Renote?.Uri ?? note.Renote?.GetPublicUriOrNull(config.Value) : null;
		var text     = quoteUri != null ? note.Text + $"\n\nRE: {quoteUri}" : note.Text;

		if (quoteUri != null)
		{
			tags.Add(new ASTagRel
			{
				Href      = new ASObjectBase(quoteUri),
				Name      = $"RE: {quoteUri}",
				Rel       = $"{Constants.MisskeyNs}#_misskey_quote",
				MediaType = Constants.ASMime
			});
		}

		var sensitive = note.Cw != null || (attachments?.OfType<ASDocument>().Any(p => p.Sensitive == true) ?? false);

		if (note.HasPoll)
		{
			var poll = await db.Polls.FirstOrDefaultAsync(p => p.Note == note);
			if (poll != null)
			{
				var closed  = poll.ExpiresAt != null && poll.ExpiresAt < DateTime.UtcNow ? poll.ExpiresAt : null;
				var endTime = poll.ExpiresAt != null && poll.ExpiresAt > DateTime.UtcNow ? poll.ExpiresAt : null;

				var choices = poll.Choices
				                  .Select(p => new ASQuestion.ASQuestionOption
				                  {
					                  Name = p,
					                  Replies = new ASCollectionBase
					                  {
						                  TotalItems =
							                  (ulong)poll.Votes[poll.Choices.IndexOf(p)]
					                  }
				                  })
				                  .ToList();

				var anyOf = poll.Multiple ? choices : null;
				var oneOf = !poll.Multiple ? choices : null;

				return new ASQuestion
				{
					Id           = id,
					AttributedTo = [new ASObjectBase(userId)],
					MkContent    = note.Text,
					PublishedAt  = note.CreatedAt,
					UpdatedAt    = note.UpdatedAt,
					Sensitive    = sensitive,
					InReplyTo    = replyId,
					Replies      = replies,
					Cc           = cc,
					To           = to,
					Tags         = tags,
					Attachments  = attachments,
					Content      = text != null ? await mfmConverter.ToHtmlAsync(text, mentions, note.UserHost) : null,
					Summary      = note.Cw,
					Source = note.Text != null
						? new ASNoteSource { Content = note.Text, MediaType = "text/x.misskeymarkdown" }
						: null,
					MkQuote     = quoteUri,
					QuoteUri    = quoteUri,
					QuoteUrl    = quoteUri,
					EndTime     = endTime,
					Closed      = closed,
					AnyOf       = anyOf,
					OneOf       = oneOf,
					VotersCount = poll.VotersCount
				};
			}
		}

		return new ASNote
		{
			Id           = id,
			AttributedTo = [new ASObjectBase(userId)],
			MkContent    = note.Text,
			PublishedAt  = note.CreatedAt,
			UpdatedAt    = note.UpdatedAt,
			Sensitive    = sensitive,
			InReplyTo    = replyId,
			Replies      = replies,
			Cc           = cc,
			To           = to,
			Tags         = tags,
			Attachments  = attachments,
			Content      = text != null ? await mfmConverter.ToHtmlAsync(text, mentions, note.UserHost) : null,
			Summary      = note.Cw,
			Source = note.Text != null
				? new ASNoteSource { Content = note.Text, MediaType = "text/x.misskeymarkdown" }
				: null,
			MkQuote  = quoteUri,
			QuoteUri = quoteUri,
			QuoteUrl = quoteUri
		};
	}
}
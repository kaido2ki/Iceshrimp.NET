using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Controllers.Pleroma.Schemas.Entities;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Conversion;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Pleroma;

[MastodonApiController]
[Authenticate]
[EnableCors("mastodon")]
[EnableRateLimiting("sliding")]
[Produces(MediaTypeNames.Application.Json)]
public class TranslationController(
	DatabaseContext db,
	MfmConverter mfmConverter,
	TranslationService translationSvc
) : ControllerBase
{
	[HttpGet("/api/v1/statuses/{id}/translations/{lang}")]
	[Authenticate("read:statuses")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<AkkomaTranslationEntity?> GetNoteTranslation(string id, string lang)
	{
		var user = HttpContext.GetUser();
		var note = await db.Notes
		                   .Where(p => p.Id == id)
		                   .IncludeCommonProperties()
		                   .FilterHidden(user, db, false, false,
		                                 filterMentions: false)
		                   .EnsureVisibleFor(user)
		                   .PrecomputeVisibilities(user)
		                   .FirstOrDefaultAsync() ??
		           throw GracefulException.RecordNotFound();

		var (translation, _) = await translationSvc.TranslateAsync(note, lang);

		return new AkkomaTranslationEntity
		{
			Text             = mfmConverter.ToHtml(translation.TranslatedText, note.MentionedRemoteUsers, note.UserHost).Html,
			DetectedLanguage = translation.OriginalLanguage
		};
	}
}
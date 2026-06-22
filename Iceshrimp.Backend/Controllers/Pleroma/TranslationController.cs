using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Controllers.Pleroma.Schemas;
using Iceshrimp.Backend.Controllers.Pleroma.Schemas.Entities;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Extensions;
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
	IServiceProvider provider
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
		
		var translationSvc = provider.GetService<ITranslationService>()
			?? throw GracefulException.UnprocessableEntity("No translation plugins have been set up");

		var translation = await translationSvc.TranslateAsync(note.Text ?? "", lang)
		                  ?? throw GracefulException.UnprocessableEntity("There was an issue translating this note");

		return new AkkomaTranslationEntity
		{
			Text             = translation.TranslatedText,
			DetectedLanguage = translation.DetectedLanguage
		};
	}
}
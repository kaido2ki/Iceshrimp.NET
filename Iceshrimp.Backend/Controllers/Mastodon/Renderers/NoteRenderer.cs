using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Conversion;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers.Mastodon.Renderers;

public class NoteRenderer(IOptions<Config.InstanceSection> config, UserRenderer userRenderer) {
	public async Task<Status> RenderAsync(Note note, int recurse = 2) {
		var uri     = note.Uri ?? $"https://{config.Value.WebDomain}/notes/@{note.Id}";
		var renote  = note.Renote != null && recurse > 0 ? await RenderAsync(note.Renote, --recurse) : null;
		var text    = note.Text; //TODO: append quote uri
		var content = text != null ? await MfmConverter.ToHtmlAsync(text) : null;

		var res = new Status {
			Id             = note.Id,
			Uri            = uri,
			Url            = note.Url ?? uri,
			Account        = await userRenderer.RenderAsync(note.User), //TODO: batch this
			ReplyId        = note.ReplyId,
			ReplyUserId    = note.ReplyUserId,
			Renote         = renote, //TODO: check if it's a pure renote
			Quote          = renote, //TODO: see above
			ContentType    = "text/x.misskeymarkdown",
			CreatedAt      = note.CreatedAt.ToString("O")[..^5],
			EditedAt       = note.UpdatedAt?.ToString("O")[..^5],
			RepliesCount   = note.RepliesCount,
			RenoteCount    = note.RenoteCount,
			FavoriteCount  = 0,     //FIXME
			IsRenoted      = false, //FIXME
			IsFavorited    = false, //FIXME
			IsBookmarked   = false, //FIXME
			IsMuted        = null,  //FIXME
			IsSensitive    = note.Cw != null,
			ContentWarning = note.Cw ?? "",
			Visibility     = Status.EncodeVisibility(note.Visibility),
			Content        = content,
			Text           = text,
			IsPinned       = false //FIXME
		};

		return res;
	}
}
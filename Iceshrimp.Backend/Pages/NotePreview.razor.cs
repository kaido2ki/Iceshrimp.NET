using Iceshrimp.Backend.Components.Helpers;
using Iceshrimp.Backend.Components.PublicPreview.Renderers;
using Iceshrimp.Backend.Components.PublicPreview.Schemas;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Pages;

public partial class NotePreview(
	MetaService meta,
	NoteRenderer renderer,
	IOptions<Config.InstanceSection> config,
	IOptionsSnapshot<Config.SecuritySection> security
) : AsyncComponentBase
{
	[Parameter] public required string Id { get; set; }

	private PreviewNote? _note;
	private string       _instanceName = "Iceshrimp.NET";

	private bool ShowMedia         => security.Value.PublicPreview > Enums.PublicPreview.RestrictedNoMedia;
	private bool ShowRemoteReplies => security.Value.PublicPreview > Enums.PublicPreview.Restricted;

	protected override async Task OnInitializedAsync()
	{
		if (security.Value.PublicPreview == Enums.PublicPreview.Lockdown)
			throw new PublicPreviewDisabledException();

		_instanceName = await meta.GetAsync(MetaEntity.InstanceName) ?? _instanceName;

		//TODO: show publish & edit timestamps
		//TODO: show quotes inline (enforce visibility by checking VisibilityIsPublicOrHome)
		//TODO: show parent post inline (enforce visibility by checking VisibilityIsPublicOrHome)
		//TODO: show avatar instead of image as fallback? can we do both?
		//TODO: thread view (respect public preview settings - don't show remote replies if set to restricted or lower)

		var note = await Database.Notes
		                         .IncludeCommonProperties()
		                         .EnsureVisibleFor(null)
		                         .FirstOrDefaultAsync(p => p.Id == Id);

		if (note is { IsPureRenote: true })
		{
			var target = note.Renote?.Url ??
			             note.Renote?.Uri ??
			             note.Renote?.GetPublicUriOrNull(config.Value) ??
			             throw new Exception("Note is remote but has no uri");

			Context.Response.Redirect(target, permanent: true);
			return;
		}

		if (note is { User.Host: not null })
		{
			var target = note.Url ?? note.Uri ?? throw new Exception("Note is remote but has no uri");
			Context.Response.Redirect(target, permanent: true);
			return;
		}

		_note = await renderer.RenderOne(note);
	}
}
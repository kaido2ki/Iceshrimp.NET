using System.Diagnostics.CodeAnalysis;
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

public partial class UserPreview(
	UserRenderer renderer,
	NoteRenderer noteRenderer,
	MetaService meta,
	IOptions<Config.InstanceSection> instance,
	IOptionsSnapshot<Config.SecuritySection> security
) : AsyncComponentBase
{
	[Parameter] public required string Acct { get; set; }

	private PreviewUser? _user;
	private string       _instanceName = "Iceshrimp.NET";
	private string?      _pronouns;
	private string       _badges = "";

	private List<PreviewNote> _pinnedNotes = [];

	private Dictionary<string, (string, string)>? _feeds;

	[SuppressMessage("ReSharper", "EntityFramework.NPlusOne.IncompleteDataQuery")]
	[SuppressMessage("ReSharper", "EntityFramework.NPlusOne.IncompleteDataUsage")]
	protected override async Task OnInitializedAsync()
	{
		if (security.Value.PublicPreview == Enums.PublicPreview.Lockdown)
			throw new PublicPreviewDisabledException();

		_instanceName = await meta.GetAsync(MetaEntity.InstanceName) ?? _instanceName;

		//TODO: user note view (respect public preview settings - don't show renotes of remote notes if set to restricted or lower)

		var split = Acct.Split("@");
		if (split.Length > 2) throw GracefulException.BadRequest("Invalid acct");
		var username = split[0].ToLowerInvariant();
		var host     = split.Length == 2 ? split[1].ToPunycodeLower() : null;

		if (host == instance.Value.AccountDomain || host == instance.Value.WebDomain)
			host = null;

		var user = await Database.Users
		                         .Include(p => p.UserSettings)
		                         .IncludeCommonProperties()
		                         .FirstOrDefaultAsync(p => p.UsernameLower == username &&
		                                                   p.Host == host &&
		                                                   !p.IsSystemUser);

		if (user is { IsRemoteUser: true })
		{
			var target = user.UserProfile?.Url ?? user.Uri ?? throw new Exception("User is remote but has no uri");
			Redirect(target);
			return;
		}

		if (user != null && security.Value.PublicPreview != Enums.PublicPreview.Lockdown)
		{
			var pinnedNotes = await Database.UserNotePins
			                                .Where(p => p.UserId == user.Id && p.Note.VisibilityIsPublicOrHome)
			                                .Select(p => p.Note)
			                                .ToListAsync();
			_pinnedNotes = await noteRenderer.RenderManyAsync(pinnedNotes);
		}

		_user     = await renderer.RenderOne(user);
		_pronouns = user?.UserProfile?.Pronouns != null
			? string.Join(", ", user.UserProfile.Pronouns.Select(p => $"{p.Value} ({p.Key.ToUpper()})"))
			: null;

		var badges = new List<string>();
		if (user?.IsAdmin ?? false) badges.Add("Admin");
		if (user?.IsModerator ?? false) badges.Add("Moderator");
		if (user?.IsBot ?? false) badges.Add("Automated");
		_badges = string.Join(" | ", badges);

		if (user is { IsLocalUser: true, UserSettings.PrivateMode: false })
		{
			_feeds = new Dictionary<string, (string, string)>
			{
				["Atom"] = ("application/atom+xml", $"/users/{user.Id}/feed.atom"),
				["JSON"] = ("application/feed+json", $"/users/{user.Id}/feed.json"),
				["RSS"] = ("application/rss+xml", $"/users/{user.Id}/feed.rss")
			};
		}
	}
}
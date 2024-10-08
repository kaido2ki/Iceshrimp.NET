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

public partial class UserPreview : AsyncComponentBase
{
	[Inject]    public required UserRenderer                             Renderer { get; set; }
	[Inject]    public required MetaService                              Meta     { get; set; }
	[Inject]    public required IOptions<Config.InstanceSection>         Instance { get; set; }
	[Inject]    public required IOptionsSnapshot<Config.SecuritySection> Security { get; set; }
	[Parameter] public required string                                   Id       { get; set; }
	[Parameter] public required string                                   Acct     { get; set; }

	private PreviewUser? _user;
	private string       _instanceName = "Iceshrimp.NET";

	[SuppressMessage("ReSharper", "EntityFramework.NPlusOne.IncompleteDataQuery")]
	[SuppressMessage("ReSharper", "EntityFramework.NPlusOne.IncompleteDataUsage")]
	protected override async Task OnInitializedAsync()
	{
		if (Security.Value.PublicPreview == Enums.PublicPreview.Lockdown)
			throw new PublicPreviewDisabledException();

		_instanceName = await Meta.Get(MetaEntity.InstanceName) ?? _instanceName;

		//TODO: user banner
		//TODO: user note view (respect public preview settings - don't show renotes of remote notes if set to restricted or lower)

		var split = Acct.Split("@");
		if (split.Length > 2) throw GracefulException.BadRequest("Invalid acct");
		var username = split[0].ToLowerInvariant();
		var host     = split.Length == 2 ? split[1].ToPunycodeLower() : null;

		if (host == Instance.Value.AccountDomain || host == Instance.Value.WebDomain)
			host = null;

		var user = await Database.Users
		                         .IncludeCommonProperties()
		                         .FirstOrDefaultAsync(p => p.UsernameLower == username &&
		                                                   p.Host == host &&
		                                                   !p.IsSystemUser);

		if (user is { IsRemoteUser: true })
		{
			var target = user.UserProfile?.Url ?? user.Uri ?? throw new Exception("User is remote but has no uri");
			Context.Response.Redirect(target, permanent: true);
			return;
		}

		_user = await Renderer.RenderOne(user);
	}
}
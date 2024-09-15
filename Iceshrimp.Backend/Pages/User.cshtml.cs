using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Conversion;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Pages;

public class UserModel(
	DatabaseContext db,
	IOptions<Config.SecuritySection> security,
	IOptions<Config.InstanceSection> instance,
	MfmConverter mfm
) : PageModel
{
	public new User?   User;
	public     string? Bio;
	public     bool    ShowMedia = security.Value.PublicPreview > Enums.PublicPreview.RestrictedNoMedia;

	[SuppressMessage("ReSharper", "EntityFramework.NPlusOne.IncompleteDataQuery",
	                 Justification = "IncludeCommonProperties")]
	[SuppressMessage("ReSharper", "EntityFramework.NPlusOne.IncompleteDataUsage", Justification = "Same as above")]
	public async Task<IActionResult> OnGet(string user, string? host)
	{
		if (Request.Cookies.ContainsKey("session") || Request.Cookies.ContainsKey("sessions"))
			return Partial("Shared/FrontendSPA");

		if (security.Value.PublicPreview == Enums.PublicPreview.Lockdown)
			throw GracefulException.Forbidden("Public preview is disabled on this instance.",
			                                  "The instance administrator has intentionally disabled this feature for privacy reasons.");

		//TODO: login button
		//TODO: user note view (respect public preview settings - don't show renotes of remote notes if set to restricted or lower)
		//TODO: emoji

		user = user.ToLowerInvariant();
		host = host?.ToLowerInvariant();

		if (host == instance.Value.AccountDomain || host == instance.Value.WebDomain)
			host = null;

		User = await db.Users.IncludeCommonProperties()
		               .Where(p => p.UsernameLower == user && p.Host == host)
		               .FirstOrDefaultAsync();

		if (User is { IsRemoteUser: true })
			return RedirectPermanent(User.UserProfile?.Url ??
			                         User.Uri ??
			                         throw new Exception("User is remote but has no uri"));

		if (User?.UserProfile?.Description is { } bio)
			Bio = await mfm.ToHtmlAsync(bio, User.UserProfile.Mentions, User.Host, divAsRoot: true);

		if (User is { AvatarUrl: null } || (User is not null && !ShowMedia))
			User.AvatarUrl = User.GetIdenticonUrl(instance.Value);

		return Page();
	}
}
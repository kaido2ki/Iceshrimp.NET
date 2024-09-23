using System.Diagnostics.CodeAnalysis;
using System.Text.Encodings.Web;
using AngleSharp;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Conversion;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Pages;

public class UserModel(
	DatabaseContext db,
	IOptions<Config.SecuritySection> security,
	IOptions<Config.InstanceSection> instance,
	MetaService meta,
	MfmConverter mfm
) : PageModel
{
	public new User?        User;
	public     string?      Bio;
	public     List<Emoji>? Emoji;
	public     bool         ShowMedia    = security.Value.PublicPreview > Enums.PublicPreview.RestrictedNoMedia;
	public     string       InstanceName = "Iceshrimp.NET";
	public     string?      DisplayName;

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

		InstanceName = await meta.Get(MetaEntity.InstanceName) ?? InstanceName;

		//TODO: user note view (respect public preview settings - don't show renotes of remote notes if set to restricted or lower)

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

		if (User != null)
		{
			Emoji       = await GetEmoji(User);
			DisplayName = HtmlEncoder.Default.Encode(User.DisplayName ?? User.Username);
			if (Emoji is { Count: > 0 })
			{
				var context  = BrowsingContext.New();
				var document = await context.OpenNewAsync();
				foreach (var emoji in Emoji)
				{
					var el    = document.CreateElement("span");
					var inner = document.CreateElement("img");
					inner.SetAttribute("src", emoji.PublicUrl);
					el.AppendChild(inner);
					el.ClassList.Add("emoji");
					DisplayName = DisplayName.Replace($":{emoji.Name.Trim(':')}:", el.OuterHtml);
				}
			}
		}

		if (User?.UserProfile?.Description is { } bio)
			Bio = await mfm.ToHtmlAsync(bio, User.UserProfile.Mentions, User.Host, divAsRoot: true, emoji: Emoji);

		if (User is { AvatarUrl: null } || (User is not null && !ShowMedia))
			User.AvatarUrl = User.GetIdenticonUrl(instance.Value);

		if (User is { Host: null })
			User.Host = instance.Value.AccountDomain;

		return Page();
	}

	private async Task<List<Emoji>> GetEmoji(User user)
	{
		var ids = user.Emojis;
		if (ids.Count == 0) return [];

		return await db.Emojis.Where(p => ids.Contains(p.Id)).ToListAsync();
	}
}
using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Pages.OAuth;

[HideRequestDuration]
public class AuthorizeModel(DatabaseContext db) : PageModel {
	[FromQuery(Name = "response_type")] public string  ResponseType { get; set; } = null!;
	[FromQuery(Name = "client_id")]     public string  ClientId     { get; set; } = null!;
	[FromQuery(Name = "redirect_uri")]  public string  RedirectUri  { get; set; } = null!;
	[FromQuery(Name = "force_login")]   public bool    ForceLogin   { get; set; } = false;
	[FromQuery(Name = "lang")]          public string? Language     { get; set; }

	[FromQuery(Name = "scope")]
	public string Scope {
		get => string.Join(' ', Scopes);
		[MemberNotNull(nameof(Scopes))] set => Scopes = value.Split(' ').ToList();
	}

	public List<string> Scopes = [];
	public OauthApp     App    = null!;
	public OauthToken?  Token  = null;

	public async Task OnGet() {
		if (ResponseType == null || ClientId == null || RedirectUri == null)
			throw GracefulException.BadRequest("Required parameters are missing or invalid");
		App = await db.OauthApps.FirstOrDefaultAsync(p => p.ClientId == ClientId.Replace(' ', '+'))
		      ?? throw GracefulException.BadRequest("Invalid client_id");
		if (MastodonOauthHelpers.ExpandScopes(Scopes).Except(MastodonOauthHelpers.ExpandScopes(App.Scopes)).Any())
			throw GracefulException.BadRequest("Cannot request more scopes than app");
		if (ResponseType != "code")
			throw GracefulException.BadRequest("Invalid response_type");
		if (!App.RedirectUris.Contains(RedirectUri))
			throw GracefulException.BadRequest("Cannot request redirect_uri not sent during app registration");
	}

	public async Task OnPost([FromForm] string username, [FromForm] string password) {
		// Validate query parameters first
		await OnGet();

		var user = await db.Users.FirstOrDefaultAsync(p => p.Host == null &&
		                                                   p.UsernameLower == username.ToLowerInvariant());
		if (user == null)
			throw GracefulException.Forbidden("Invalid username or password");
		var userProfile = await db.UserProfiles.FirstOrDefaultAsync(p => p.User == user);
		if (userProfile?.Password == null)
			throw GracefulException.Forbidden("Invalid username or password");
		if (AuthHelpers.ComparePassword(password, userProfile.Password) == false)
			throw GracefulException.Forbidden("Invalid username or password");

		var token = new OauthToken {
			Id          = IdHelpers.GenerateSlowflakeId(),
			Active      = false,
			Code        = CryptographyHelpers.GenerateRandomString(32),
			Token       = CryptographyHelpers.GenerateRandomString(32),
			App         = App,
			User        = user,
			CreatedAt   = DateTime.UtcNow,
			Scopes      = Scopes,
			RedirectUri = RedirectUri
		};

		await db.AddAsync(token);
		await db.SaveChangesAsync();

		Token = token;
	}
}
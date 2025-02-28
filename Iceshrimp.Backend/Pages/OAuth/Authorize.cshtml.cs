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
public class AuthorizeModel(DatabaseContext db) : PageModel
{
	public OauthApp App = null!;

	public                                     List<string> Scopes = [];
	public                                     OauthToken?  Token;
	public                                     List<User>   AuthenticatedUsers = [];
	[FromQuery(Name = "response_type")] public string       ResponseType { get; set; } = null!;
	[FromQuery(Name = "client_id")]     public string       ClientId     { get; set; } = null!;
	[FromQuery(Name = "redirect_uri")]  public string       RedirectUri  { get; set; } = null!;
	[FromQuery(Name = "force_login")]   public bool         ForceLogin   { get; set; } = false;
	[FromQuery(Name = "lang")]          public string?      Language     { get; set; }

	[FromQuery(Name = "scope")]
	public string Scope
	{
		get => string.Join(' ', Scopes);
		[MemberNotNull(nameof(Scopes))] set => Scopes = value.Split(' ').ToList();
	}

	public LoginData? TwoFactorFormData;

	public async Task OnGet()
	{
		if (ResponseType == null || ClientId == null || RedirectUri == null)
			throw GracefulException.BadRequest("Required parameters are missing or invalid");
		App = await db.OauthApps.FirstOrDefaultAsync(p => p.ClientId == ClientId.Replace(' ', '+')) ??
		      throw GracefulException.BadRequest("Invalid client_id");
		if (MastodonOauthHelpers.ExpandScopes(Scopes).Except(MastodonOauthHelpers.ExpandScopes(App.Scopes)).Any())
			throw GracefulException.BadRequest("Cannot request more scopes than app");
		if (ResponseType != "code")
			throw GracefulException.BadRequest("Invalid response_type");
		if (!App.RedirectUris.Contains(RedirectUri))
			throw GracefulException.BadRequest("Cannot request redirect_uri not sent during app registration");
		if (Request.Cookies.TryGetValue("sessions", out var sessions))
		{
			var tokens = sessions.Split(',');
			AuthenticatedUsers = await db.Sessions
			                             .Where(p => tokens.Contains(p.Token) && p.Active)
			                             .Select(p => p.User)
			                             .ToListAsync();
		}
	}

	public async Task OnPost(
		[FromForm] string? username, [FromForm] string? password, [FromForm] string? totp, [FromForm] string? userId,
		[FromForm] bool supportsHtmlFormatting, [FromForm] bool autoDetectQuotes, [FromForm] bool isPleroma
	)
	{
		// Validate query parameters & populate model first
		await OnGet();

		var user = AuthenticatedUsers.FirstOrDefault(p => p.Id == userId);

		if (user == null)
		{
			Exception Forbidden() => GracefulException.Forbidden("Invalid username or password");
			if (username == null || password == null)
				throw Forbidden();

			user = await db.Users.FirstOrDefaultAsync(p => p.IsLocalUser &&
			                                               p.UsernameLower == username.ToLowerInvariant()) ??
			       throw Forbidden();
			if (user.IsSystemUser) throw GracefulException.BadRequest("Cannot log in as system user");
			var userSettings = await db.UserSettings.FirstOrDefaultAsync(p => p.User == user);
			if (userSettings?.Password == null)
				throw Forbidden();
			if (AuthHelpers.ComparePassword(password, userSettings.Password) == false)
				throw Forbidden();

			if (userSettings.TwoFactorEnabled)
			{
				if (!string.IsNullOrWhiteSpace(totp))
				{
					if (userSettings.TwoFactorSecret == null)
						throw new Exception("2FA is enabled but secret is null");
					if (!TotpHelper.Validate(userSettings.TwoFactorSecret, totp))
					{
						SetTwoFactorFormData();
						return;
					}
				}
				else
				{
					SetTwoFactorFormData();
					return;
				}
			}
		}

		var token = new OauthToken
		{
			Id                     = IdHelpers.GenerateSnowflakeId(),
			Active                 = false,
			Code                   = CryptographyHelpers.GenerateRandomString(32),
			Token                  = CryptographyHelpers.GenerateRandomString(32),
			App                    = App,
			User                   = user,
			CreatedAt              = DateTime.UtcNow,
			Scopes                 = Scopes,
			RedirectUri            = RedirectUri,
			AutoDetectQuotes       = autoDetectQuotes,
			SupportsHtmlFormatting = supportsHtmlFormatting,
			IsPleroma              = isPleroma
		};

		await db.AddAsync(token);
		await db.SaveChangesAsync();

		Token = token;
		return;

		void SetTwoFactorFormData()
		{
			TwoFactorFormData = new LoginData
			{
				Username               = username,
				Password               = password,
				AutoDetectQuotes       = autoDetectQuotes,
				SupportsHtmlFormatting = supportsHtmlFormatting,
				IsPleroma              = isPleroma
			};

			Response.Headers.CacheControl = "private, no-store, no-cache";
			Response.Headers.Pragma       = "no-cache";
		}
	}

	public class LoginData
	{
		public required string Username;
		public required string Password;
		public required bool   SupportsHtmlFormatting;
		public required bool   AutoDetectQuotes;
		public required bool   IsPleroma;
	}
}
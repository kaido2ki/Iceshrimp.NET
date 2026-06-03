using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Controllers.Web.Renderers;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers.Web;

/// <summary>
/// Operations for getting metadata from the instance.
/// </summary>
[ApiController]
[EnableRateLimiting("sliding")]
[Route("/api/iceshrimp/instance")]
[Produces(MediaTypeNames.Application.Json)]
[EnableCors("iceshrimp")]
public class InstanceController(
	DatabaseContext db,
	UserRenderer userRenderer,
	IOptions<Config.InstanceSection> instanceConfig,
	IOptionsSnapshot<Config.SecuritySection> securityConfig,
	MetaService meta,
	InstanceService instanceSvc
) : ControllerBase
{
	/// <summary>
	/// Get metadata
	/// </summary>
	/// <remarks>Returns metadata about the instance.</remarks>
	/// <response code="200">Instance metadata</response>
	[HttpGet]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<InstanceResponse> GetInfo()
	{
		var limits = new Limitations { NoteLength = instanceConfig.Value.CharacterLimit };

		// Have to do multiple gets because of the amount of things being deconstructed
		var (instanceName, iconId, bannerId, themeColor) =
			await meta.GetManyAsync(MetaEntity.InstanceName, MetaEntity.IconFileId, MetaEntity.BannerFileId,
			                        MetaEntity.ThemeColor);
		var (description, contactEmail) = await meta.GetManyAsync(MetaEntity.InstanceDescription, MetaEntity.AdminContactEmail);
		
		// This is separate because it is not nullable while the above values are nullable
		var vapidKey = await meta.GetAsync(MetaEntity.VapidPublicKey);

		var iconUrl = await db.DriveFiles.Where(p => p.Id == iconId)
		                      .Select(p => p.PublicUrl ?? p.RawAccessUrl)
		                      .FirstOrDefaultAsync();
		var bannerUrl = await db.DriveFiles.Where(p => p.Id == bannerId)
		                        .Select(p => p.PublicUrl ?? p.RawAccessUrl)
		                        .FirstOrDefaultAsync();

		return new InstanceResponse
		{
			AccountDomain = instanceConfig.Value.AccountDomain,
			WebDomain     = instanceConfig.Value.WebDomain,
			Registration  = (Registrations)securityConfig.Value.Registrations,
			VapidKey      = vapidKey,
			Name          = instanceName ?? instanceConfig.Value.AccountDomain,
			IconUrl       = iconUrl,
			BannerUrl     = bannerUrl,
			ThemeColor    = themeColor,
			Limits        = limits,
			UserCount     = await db.Users.CountAsync(p => p.Host == null && !p.IsSuspended && !p.IsSystemUser),
			Description   = description,
			ContactEmail  = contactEmail
		};
	}

	/// <summary>
	/// List rules
	/// </summary>
	/// <remarks>Returns a list of rules defined by the admins.</remarks>
	/// <response code="200">List of rules</response>
	[HttpGet("rules")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<List<RuleResponse>> GetRules()
	{
		return await db.Rules
		               .OrderBy(p => p.Order)
		               .ThenBy(p => p.Id)
		               .Select(p => new RuleResponse { Id = p.Id, Text = p.Text, Description = p.Description })
		               .ToListAsync();
	}

	/// <summary>
	/// Create rule
	/// </summary>
	/// <remarks>Requires role: <b>Admin</b></remarks>
	/// <param name="request">Create rule request</param>
	/// <response code="200">New rule</response>
	[HttpPost("rules")]
	[Authenticate]
	[Authorize("role:admin")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<RuleResponse> CreateRule(RuleCreateRequest request)
	{
		var rule = await instanceSvc.CreateRuleAsync(request.Text.Trim(), request.Description?.Trim());

		return new RuleResponse { Id = rule.Id, Text = rule.Text, Description = rule.Description };
	}

	/// <summary>
	/// Update rule
	/// </summary>
	/// <remarks>Requires role: <b>Admin</b></remarks>
	/// <param name="id">The rule's ID</param>
	/// <param name="request">Update rule request. Fields that are not present are not updated. Fields that are empty are reset.</param>
	/// <response code="200">Updated rule</response>
	[HttpPatch("rules/{id}")]
	[Authenticate]
	[Authorize("role:admin")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<RuleResponse> UpdateRule(string id, RuleUpdateRequest request)
	{
		var rule = await db.Rules.FirstOrDefaultAsync(p => p.Id == id)
		           ?? throw GracefulException.RecordNotFound();

		var order = request.Order ?? 0;
		var text  = request.Text?.Trim() ?? rule.Text;
		var description = request.Description != null
			? string.IsNullOrWhiteSpace(request.Description)
				? null
				: request.Description.Trim()
			: rule.Description;

		var res = await instanceSvc.UpdateRuleAsync(rule, order, text, description);

		return new RuleResponse { Id = res.Id, Text = res.Text, Description = res.Description };
	}

	/// <summary>
	/// Delete rule
	/// </summary>
	/// <remarks>Requires role: <b>Admin</b></remarks>
	/// <param name="id">The rule's ID</param>
	[HttpDelete("rules/{id}")]
	[Authenticate]
	[Authorize("role:admin")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task DeleteRule(string id)
	{
		var rule = await db.Rules.FirstOrDefaultAsync(p => p.Id == id)
		           ?? throw GracefulException.RecordNotFound();

		var rules = await db.Rules
		                    .Where(p => p.Order > rule.Order)
		                    .ToListAsync();

		db.Remove(rule);
		
		foreach (var r in rules)
			r.Order -= 1;
		db.UpdateRange(rules);
		await db.SaveChangesAsync();
	}

	/// <summary>
	/// Get staff
	/// </summary>
	/// <remarks>Returns a list of admins and a list of moderators on the instance.</remarks>
	/// <response code="200">Staff information</response>
	[HttpGet("staff")]
	[Authenticate]
	[Authorize]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<StaffResponse> GetStaff()
	{
		var admins = await db.Users
					   .Where(p => p.IsAdmin == true)
					   .OrderBy(p => p.UsernameLower)
					   .ToListAsync();
		var adminList = await userRenderer.RenderManyAsync(admins)
										  .ToListAsync();

		var moderators = await db.Users
						   .Where(p => p.IsAdmin == false && p.IsModerator == true)
						   .OrderBy(p => p.UsernameLower)
						   .ToListAsync();
		var moderatorList = await userRenderer.RenderManyAsync(moderators)
											  .ToListAsync();

		return new StaffResponse { Admins = adminList, Moderators = moderatorList };
	}

	// This is only used to set the icon for the frontend
	[ApiExplorerSettings(IgnoreApi = true)]
	[HttpGet("/favicon.png")]
	[ProducesResults(HttpStatusCode.Redirect)]
	public async Task<RedirectResult> GetInstanceIcon()
	{
		var iconId  = await meta.GetAsync(MetaEntity.IconFileId);
		var iconUrl = iconId != null
			? await db.DriveFiles.Where(p => p.Id == iconId).Select(p => p.PublicUrl).FirstOrDefaultAsync()
			: null;

		return new RedirectResult(iconUrl ?? $"https://{instanceConfig.Value.WebDomain}/_content/Iceshrimp.Assets.Branding/favicon.png");
	}
}

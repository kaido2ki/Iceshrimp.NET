using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Controllers.Web.Renderers;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers.Web;

[ApiController]
[EnableRateLimiting("sliding")]
[Route("/api/iceshrimp/instance")]
[Produces(MediaTypeNames.Application.Json)]
public class InstanceController(
	DatabaseContext db,
	UserRenderer userRenderer,
	IOptions<Config.InstanceSection> instanceConfig,
	IOptions<Config.SecuritySection> securityConfig,
	MetaService meta
) : ControllerBase
{
	[HttpGet]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<InstanceResponse> GetInfo()
	{
		var limits = new Limitations { NoteLength = instanceConfig.Value.CharacterLimit };

		return new InstanceResponse
		{
			AccountDomain = instanceConfig.Value.AccountDomain,
			WebDomain     = instanceConfig.Value.WebDomain,
			Registration  = (Registrations)securityConfig.Value.Registrations,
			Name          = await meta.GetAsync(MetaEntity.InstanceName),
			Limits        = limits
		};
	}

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

	[HttpPost("rules")]
	[Authenticate]
	[Authorize("role:moderator")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<RuleResponse> CreateRule(RuleCreateRequest request)
	{
		var count = await db.Rules.CountAsync();

		var rule = new Rule
		{
			Id          = IdHelpers.GenerateSnowflakeId(),
			Order       = count + 1,
			Text        = request.Text,
			Description = request.Description
		};

		db.Add(rule);
		await db.SaveChangesAsync();

		return new RuleResponse { Id = rule.Id, Text = rule.Text, Description = rule.Description };
	}

	[HttpPatch("rules/{id}")]
	[Authenticate]
	[Authorize("role:moderator")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<RuleResponse> UpdateRule(string id, RuleUpdateRequest request)
	{
		var rule = await db.Rules.FirstOrDefaultAsync(p => p.Id == id)
		           ?? throw GracefulException.RecordNotFound();

		var count = await db.Rules.CountAsync();
		// order is defined here because I don't know why but request.Order is still nullable even if the if statement checks it isn't null
		var order = request.Order ?? 0;
		if (order > 0 && order != rule.Order && count != 1)
		{
			request.Order = Math.Min(order, count);
			
			if (order > rule.Order)
			{
				var rules = await db.Rules
				              .Where(p => rule.Order < p.Order && p.Order <= order)
				              .ToListAsync();

				foreach (var r in rules)
					r.Order -= 1;

				db.UpdateRange(rules);
			}
			else
			{
				var rules = await db.Rules
				                    .Where(p => order <= p.Order && p.Order < rule.Order)
				                    .ToListAsync();

				foreach (var r in rules)
					r.Order += 1;

				db.UpdateRange(rules);
			}
			
			rule.Order = order;
		}

		if (request.Text != null)
			rule.Text = request.Text.Trim();

		if (request.Description != null)
			rule.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

		db.Update(rule);
		await db.SaveChangesAsync();

		return new RuleResponse { Id = rule.Id, Text = rule.Text, Description = rule.Description };
	}

	[HttpDelete("rules/{id}")]
	[Authenticate]
	[Authorize("role:moderator")]
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

	[HttpGet("staff")]
	[Authenticate]
	[Authorize]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<StaffResponse> GetStaff()
	{
		var admins = db.Users
					   .Where(p => p.IsAdmin == true)
					   .OrderBy(p => p.UsernameLower);
		var adminList = await userRenderer.RenderManyAsync(admins)
										  .ToListAsync();

		var moderators = db.Users
						   .Where(p => p.IsAdmin == false && p.IsModerator == true)
						   .OrderBy(p => p.UsernameLower);
		var moderatorList = await userRenderer.RenderManyAsync(moderators)
											  .ToListAsync();

		return new StaffResponse { Admins = adminList, Moderators = moderatorList };
	}
}

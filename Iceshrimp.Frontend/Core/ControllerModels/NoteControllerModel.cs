using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Iceshrimp.Frontend.Core.Extensions;
using Iceshrimp.Shared.Schemas;
using Microsoft.AspNetCore.Http;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class NoteControllerModel(HttpClient api)
{
	public Task<NoteResponse?> GetNote(string id) => api.CallNullable<NoteResponse>(HttpMethod.Get, $"/note/{id}");

	public Task<NoteResponse?> GetNoteAscendants(string id, [DefaultValue(20)] [Range(1, 100)] int? limit)
	{
		var query = new QueryString();
		if (limit.HasValue) query.Add("limit", limit.ToString());
		return api.CallNullable<NoteResponse>(HttpMethod.Get, $"/note/{id}/ascendants", query);
	}

	public Task<NoteResponse?> GetNoteDescendants(string id, [DefaultValue(20)] [Range(1, 100)] int? depth)
	{
		var query = new QueryString();
		if (depth.HasValue) query.Add("depth", depth.ToString());
		return api.CallNullable<NoteResponse>(HttpMethod.Get, $"/note/{id}/descendants", query);
	}
}
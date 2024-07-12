using System.ComponentModel.DataAnnotations;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JR = System.Text.Json.Serialization.JsonRequiredAttribute;
using B = Microsoft.AspNetCore.Mvc.BindPropertyAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas;

public class FilterSchemas
{
	public class CreateFilterRequest
	{
		[MinLength(1)]
		[B(Name = "title")]
		[J("title")]
		[JR]
		public required string Title { get; set; }

		[B(Name = "context")]
		[J("context")]
		[JR]
		public required List<string> Context { get; set; }

		[B(Name = "filter_action")]
		[J("filter_action")]
		[JR]
		public required string Action { get; set; }

		[B(Name = "expires_in")]
		[J("expires_in")]
		public long? ExpiresIn { get; set; }

		[B(Name = "keywords_attributes")]
		[J("keywords_attributes")]
		public List<FilterKeywordsAttributes> Keywords { get; set; } = [];
	}

	public class UpdateFilterRequest : CreateFilterRequest
	{
		[B(Name = "keywords_attributes")]
		[J("keywords_attributes")]
		public new List<UpdateFilterKeywordsAttributes> Keywords { get; set; } = [];
	}

	public class FilterKeywordsAttributes
	{
		[B(Name = "keyword")]
		[J("keyword")]
		[JR]
		public required string Keyword { get; set; }

		[B(Name = "whole_word")]
		[J("whole_word")]
		public bool WholeWord { get; set; } = false;
	}

	public class UpdateFilterKeywordsAttributes : FilterKeywordsAttributes
	{
		[B(Name = "id")] [J("id")]             public string? Id      { get; set; }
		[B(Name = "_destroy")] [J("_destroy")] public bool    Destroy { get; set; } = false;
	}
}
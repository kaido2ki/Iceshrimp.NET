using Microsoft.AspNetCore.Mvc;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using B = Microsoft.AspNetCore.Mvc.BindPropertyAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas;

public abstract class MediaSchemas
{
	public class UploadMediaRequest
	{
		[B(Name = "file")]               public required IFormFile File        { get; set; }
		[FromForm(Name = "description")] public          string?   Description { get; set; }

		//TODO: add thumbnail & focus properties
	}

	public class UpdateMediaRequest
	{
		[J("description")]
		[B(Name = "description")]
		public string? Description { get; set; }

		//TODO: add thumbnail & focus properties
	}
}
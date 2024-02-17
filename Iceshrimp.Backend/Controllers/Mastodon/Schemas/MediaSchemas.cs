using Microsoft.AspNetCore.Mvc;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas;

public abstract class MediaSchemas
{
	public class UploadMediaRequest
	{
		[FromForm(Name = "file")]        public required IFormFile File        { get; set; }
		[FromForm(Name = "description")] public          string?   Description { get; set; }

		//TODO: add thumbnail & focus properties
	}
}
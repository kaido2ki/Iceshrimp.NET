using System.IO.Compression;
using System.Net;
using System.Text.Json;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.AspNetCore.StaticFiles;

namespace Iceshrimp.Backend.Core.Services;

public class EmojiZipEmoji
{
	public string?      Name     { get; set; }
	public string?      Category { get; set; }
	public List<string> Aliases  { get; set; } = [];
}

public class EmojiZipEntry
{
	public required string        FileName { get; set; }
	public required EmojiZipEmoji Emoji    { get; set; }
}

public class EmojiZipMeta
{
	public required ushort          MetaVersion { get; set; }
	public required EmojiZipEntry[] Emojis      { get; set; }
}

public record EmojiZip(EmojiZipMeta Metadata, ZipArchive Archive);

public class EmojiImportService(
	EmojiService emojiSvc,
	ILogger<EmojiImportService> logger
) : IScopedService
{
	public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

	public async Task<EmojiZip> Parse(Stream zipStream)
	{
		var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

		try
		{
			var meta = archive.GetEntry("meta.json") ??
			           throw GracefulException
				           .BadRequest("Invalid emoji zip. Only Misskey-style emoji zips are supported.");

			var metaJson = await JsonSerializer.DeserializeAsync<EmojiZipMeta>(meta.Open(), SerializerOptions) ??
			               throw GracefulException.BadRequest("Invalid emoji zip metadata");

			if (metaJson.MetaVersion < 1 || metaJson.MetaVersion > 2)
				throw GracefulException.BadRequest("Unrecognized metaVersion {version}, expected 1 or 2",
				                                   metaJson.MetaVersion.ToString());

			return new EmojiZip(metaJson, archive);
		}
		catch
		{
			// We don't want to dispose of archive on success, as Import will do it when it's done.
			archive.Dispose();
			throw;
		}
	}

	public async Task Import(EmojiZip zip)
	{
		using var archive             = zip.Archive;
		var       contentTypeProvider = new FileExtensionContentTypeProvider();

		foreach (var emoji in zip.Metadata.Emojis)
		{
			var file = archive.GetEntry(emoji.FileName);
			if (file == null)
			{
				logger.LogWarning("Skipping {file} as no such file was found in the zip.", emoji.FileName);
				continue;
			}

			if (!contentTypeProvider.TryGetContentType(emoji.FileName, out var mimeType))
			{
				logger.LogWarning("Skipping {file} as the mime type could not be detemrined.", emoji.FileName);
				continue;
			}

			// DriveService requires a seekable and .Length-able stream, which the DeflateStream from file.Open does not support.
			using var buffer = new MemoryStream((int)file.Length);
			await file.Open().CopyToAsync(buffer);
			buffer.Seek(0, SeekOrigin.Begin);

			var name = emoji.Emoji.Name ?? emoji.FileName;

			try
			{
				await emojiSvc.CreateEmojiFromStream(
				                                     buffer,
				                                     name,
				                                     mimeType,
				                                     emoji.Emoji.Aliases,
				                                     emoji.Emoji.Category
				                                    );

				logger.LogDebug("Imported emoji {emoji}", name);
			}
			catch (GracefulException e) when (e.StatusCode == HttpStatusCode.Conflict)
			{
				logger.LogDebug("Skipping {emoji} as it already exists.", name);
			}
		}
	}
}
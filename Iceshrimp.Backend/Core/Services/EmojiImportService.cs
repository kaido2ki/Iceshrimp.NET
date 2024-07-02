using System.IO.Compression;
using System.Text.Json;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using static Iceshrimp.Backend.Core.Configuration.Config;

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
    ILogger<EmojiImportService> logger, 
    IOptions<InstanceSection> config
)
{
    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<EmojiZip> Parse(Stream zipStream)
    {
        var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

        try
        {
            var meta = archive.GetEntry("meta.json")
                ?? throw GracefulException.BadRequest("Invalid emoji zip. Only Misskey-style emoji zips are supported.");

            var metaJson = await JsonSerializer.DeserializeAsync<EmojiZipMeta>(meta.Open(), SerializerOptions)
                ?? throw GracefulException.BadRequest("Invalid emoji zip metadata");

            if (metaJson.MetaVersion < 1 || metaJson.MetaVersion > 2)
                throw GracefulException.BadRequest("Unrecognized metaVersion {Version}, expected 1 or 2", metaJson.MetaVersion.ToString());

            return new(metaJson, archive);
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
        using var archive = zip.Archive;
        var contentTypeProvider = new FileExtensionContentTypeProvider();

        foreach (var emoji in zip.Metadata.Emojis)
        {
            var file = archive.GetEntry(emoji.FileName);
            if (file == null)
            {
                logger.LogWarning("Skipping {File} as no such file was found in the zip.", emoji.FileName);
                continue;
            }

            if (!contentTypeProvider.TryGetContentType(emoji.FileName, out var mimeType))
            {
                logger.LogWarning("Skipping {File} the mime type could not be detemrined.", emoji.FileName);
                continue;
            }

            // DriveService requires a seekable and .Length-able stream, which the DeflateStream from file.Open does not support.
            using var buffer = new MemoryStream((int)file.Length);
            await file.Open().CopyToAsync(buffer);
            buffer.Seek(0, SeekOrigin.Begin);

            logger.LogDebug("Importing emoji {Emoji}", emoji.Emoji.Name ?? emoji.FileName);
            await emojiSvc.CreateEmojiFromStream(
                buffer,
                emoji.Emoji.Name ?? emoji.FileName,
                mimeType,
                config.Value,
                emoji.Emoji.Aliases,
                emoji.Emoji.Category
            );
        }
    }
}
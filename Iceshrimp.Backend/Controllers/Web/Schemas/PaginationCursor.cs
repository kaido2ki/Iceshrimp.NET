using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Shared.Schemas.Web;
using MessagePack;
using Microsoft.AspNetCore.WebUtilities;

namespace Iceshrimp.Backend.Controllers.Web.Schemas;

public class PaginationCursor
{
	/// <summary>
	///     The ID of the entity we're paginating a list of. Will almost always be required as a
	///     tiebreaker
	/// </summary>
	[Key(0)]
	public required string Id { get; init; }

	/// <summary>
	///     Flag to indicate this cursor is pointing "up" (as in,
	///     <see cref="PaginationWrapper{T}.PageUp" />). If this flag is true, the results should
	///     be reversed when fetching from the database (it will be un-reversed later)
	/// </summary>
	[Key(1)]
	public bool Up { get; init; }
}

static class PaginationCursorExtensions
{
	public static string Serialize<T>(this T cursor)
		where T : PaginationCursor =>
		Base64UrlTextEncoder.Encode(MessagePackSerializer.Serialize(cursor));

	public static T ParseCursor<T>(this string value)
		where T : PaginationCursor
	{
		try
		{
			return MessagePackSerializer.Deserialize<T>(Base64UrlTextEncoder.Decode(value));
		}
		catch (MessagePackSerializationException)
		{
			throw GracefulException.BadRequest("Invalid cursor value.");
		}
	}
}

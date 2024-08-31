using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Pleroma.Schemas.Entities;

public class PleromaStatusExtensions 
{
	[J("emoji_reactions")] public required List<ReactionEntity> Reactions      { get; set; }
	[J("conversation_id")] public required string               ConversationId { get; set; }
}
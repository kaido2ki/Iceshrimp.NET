using System.Text.Json.Serialization;
using JI = System.Text.Json.Serialization.JsonIgnoreAttribute;

namespace Iceshrimp.Shared.Schemas.Web;

public class NotificationResponse
{
	public required string        Id        { get; set; }
	public required string        Type      { get; set; }
	public required bool          Read      { get; set; }
	public required string        CreatedAt { get; set; }
	public          NoteResponse? Note      { get; set; }
	public          UserResponse? User      { get; set; }
	[JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public          BiteResponse? Bite    { get; set; }
	
	[JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public          ReactionResponse? Reaction { get; set; }
	
	public class BiteResponse
	{
		public required string Id       { get; set; }
		public required bool   BiteBack { get; set; }
	}

	public class ReactionResponse
	{
		public required string  Name      { get; set; }
		public required string? Url       { get; set; }
		public required bool    Sensitive { get; set; }
	}
}


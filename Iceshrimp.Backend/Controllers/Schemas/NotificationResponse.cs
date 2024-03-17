using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Schemas;

public class NotificationResponse
{
	[J("id")]        public required string        Id        { get; set; }
	[J("type")]      public required string        Type      { get; set; }
	[J("read")]      public required bool          Read      { get; set; }
	[J("createdAt")] public required string        CreatedAt { get; set; }
	[J("note")]      public          NoteResponse? Note      { get; set; }
	[J("user")]      public          UserResponse? User      { get; set; }
}
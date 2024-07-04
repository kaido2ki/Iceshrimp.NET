namespace Iceshrimp.Shared.Schemas.Web;

public class NotificationResponse
{
	public required string        Id        { get; set; }
	public required string        Type      { get; set; }
	public required bool          Read      { get; set; }
	public required string        CreatedAt { get; set; }
	public          NoteResponse? Note      { get; set; }
	public          UserResponse? User      { get; set; }
}
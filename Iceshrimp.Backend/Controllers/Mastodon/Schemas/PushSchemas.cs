using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JR = System.Text.Json.Serialization.JsonRequiredAttribute;
using B = Microsoft.AspNetCore.Mvc.BindPropertyAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas;

public abstract class PushSchemas
{
	public class PushSubscription
	{
		[J("id")]         public required string Id        { get; set; }
		[J("endpoint")]   public required string Endpoint  { get; set; }
		[J("server_key")] public required string ServerKey { get; set; }
		[J("alerts")]     public required Alerts Alerts    { get; set; }
		[J("policy")]     public required string Policy    { get; set; }
	}

	public class Alerts
	{
		[J("mention")] [B(Name = "mention")] public bool Mention { get; set; } = false;
		[J("status")] [B(Name = "status")]   public bool Status  { get; set; } = false;
		[J("reblog")] [B(Name = "reblog")]   public bool Reblog  { get; set; } = false;
		[J("follow")] [B(Name = "follow")]   public bool Follow  { get; set; } = false;

		[J("follow_request")]
		[B(Name = "follow_request")]
		public bool FollowRequest { get; set; } = false;

		[J("favourite")]
		[B(Name = "favourite")]
		public bool Favourite { get; set; } = false;

		[J("poll")] [B(Name = "poll")]     public bool Poll   { get; set; } = false;
		[J("update")] [B(Name = "update")] public bool Update { get; set; } = false;
	}

	public class RegisterPushRequest
	{
		[J("subscription")]
		[JR]
		[B(Name = "subscription")]
		public required Subscription Subscription { get; set; }

		[J("data")] [B(Name = "data")] public RegisterPushRequestData Data { get; set; } = new();
	}

	public class RegisterPushRequestData
	{
		[J("alerts")] [B(Name = "alerts")] public Alerts Alerts { get; set; } = new();
		[J("policy")] [B(Name = "policy")] public string Policy { get; set; } = "all";
	}

	public class EditPushRequest
	{
		[J("policy")]
		[B(Name = "policy")]
		public string Policy
		{
			get => Data.Policy;
			set => Data.Policy = value;
		}

		[J("data")] [B(Name = "data")] public RegisterPushRequestData Data { get; set; } = new();
	}

	public class EditPushRequestData
	{
		[J("alerts")] [B(Name = "alerts")] public Alerts Alerts { get; set; } = new();
		[J("policy")] [B(Name = "policy")] public string Policy { get; set; } = "all";
	}

	public class Subscription
	{
		[J("endpoint")]
		[JR]
		[B(Name = "endpoint")]
		public required string Endpoint { get; set; }

		[J("keys")] [JR] [B(Name = "keys")] public required Keys Keys { get; set; }
	}

	public class Keys
	{
		[J("p256dh")]
		[JR]
		[B(Name = "p256dh")]
		public required string PublicKey { get; set; }

		[J("auth")] [JR] [B(Name = "auth")] public required string AuthSecret { get; set; }
	}

	public class PushNotification
	{
		[J("access_token")]      public required string AccessToken      { get; set; }
		[J("notification_id")]   public required long   NotificationId   { get; set; }
		[J("notification_type")] public required string NotificationType { get; set; }
		[J("icon")]              public required string IconUrl          { get; set; }
		[J("title")]             public required string Title            { get; set; }
		[J("body")]              public required string Body             { get; set; }
		[J("preferred_locale")]  public          string PreferredLocale  => "en";
	}
}
using System.Text.Json.Serialization;

namespace Iceshrimp.Shared.Schemas;

public class UserProfileResponse
{
	public required string                  Id        { get; set; }
	public required string?                 Birthday  { get; set; }
	public required string?                 Location  { get; set; }
	public required List<UserProfileField>? Fields    { get; set; }
	public required string?                 Bio       { get; set; }
	public required int?                    Followers { get; set; }
	public required int?                    Following { get; set; }
	public required RelationData            Relations { get; set; }
}

public class RelationData
{
	[JsonIgnore] public required string UserId;

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public required bool IsSelf { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public required bool IsFollowing { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public required bool IsFollowedBy { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public required bool IsRequested { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public required bool IsRequestedBy { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public required bool IsBlocking { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public required bool IsMuting { get; set; }
}

public class UserProfileField
{
	public required string Name     { get; set; }
	public required string Value    { get; set; }
	public          bool?  Verified { get; set; }
}
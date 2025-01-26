using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Iceshrimp.Shared.Schemas.Web;

public class UserProfileEntity
{
	[MaxLength(2048)] public string? Description { get; set; }
	[MaxLength(128)]  public string? Location    { get; set; }

	/// <remarks>
	///     Accepts YYYY-MM-DD format, empty string, or null.
	/// </remarks>
	public string? Birthday { get; set; }

	//TODO: public string? Lang { get; set; }

	public required List<Field>      Fields       { get; set; }
	public required FFVisibilityEnum FFVisibility { get; set; }
	public required string           DisplayName  { get; set; }
	public          string?          AvatarAlt    { get; set; }
	public          string?          BannerAlt    { get; set; }

	public required bool IsBot      { get; set; }
	public required bool IsCat      { get; set; }
	public required bool SpeakAsCat { get; set; }

	[MaxLength(128)] public required string? Pronouns { get; set; }

	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	public enum FFVisibilityEnum
	{
		Public    = 0,
		Followers = 1,
		Private   = 2
	}

	public class Field
	{
		public required string Name  { get; set; }
		public required string Value { get; set; }
	}
}
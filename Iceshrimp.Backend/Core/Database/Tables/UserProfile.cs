﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("user_profile")]
[Index(nameof(UserHost))]
[Index(nameof(PinnedPageId), IsUnique = true)]
public class UserProfile
{
	[Column("mentionsResolved")] public bool MentionsResolved;

	[Key]
	[Column("userId")]
	[StringLength(32)]
	public string UserId { get; set; } = null!;

	/// <summary>
	///     The location of the User.
	/// </summary>
	[Column("location")]
	[StringLength(128)]
	public string? Location { get; set; }

	/// <summary>
	///     The birthday (YYYY-MM-DD) of the User.
	/// </summary>
	[Column("birthday")]
	[StringLength(10)]
	public string? Birthday { get; set; }

	/// <summary>
	///     The description (bio) of the User.
	/// </summary>
	[Column("description")]
	[StringLength(2048)]
	public string? Description { get; set; }

	[Column("fields", TypeName = "jsonb")] public Field[] Fields { get; set; } = null!;

	/// <summary>
	///     Remote URL of the user.
	/// </summary>
	[Column("url")]
	[StringLength(512)]
	public string? Url { get; set; }

	[Column("ffVisibility")] public UserProfileFFVisibility FFVisibility { get; set; }

	/// <summary>
	///     [Denormalized]
	/// </summary>
	[Column("userHost")]
	[StringLength(512)]
	public string? UserHost { get; set; }

	[Column("pinnedPageId")]
	[StringLength(32)]
	public string? PinnedPageId { get; set; }

	[Column("lang")] [StringLength(32)] public string? Lang { get; set; }

	[Column("moderationNote")]
	[StringLength(8192)]
	public string ModerationNote { get; set; } = null!;

	[Column("mentions", TypeName = "jsonb")]
	public List<Note.MentionedUser> Mentions { get; set; } = null!;

	[ForeignKey(nameof(PinnedPageId))]
	[InverseProperty(nameof(Page.UserProfile))]
	public virtual Page? PinnedPage { get; set; }

	[ForeignKey(nameof(UserId))]
	[InverseProperty(nameof(Tables.User.UserProfile))]
	public virtual User User { get; set; } = null!;

	public class Field
	{
		[J("name")]     public required string Name       { get; set; }
		[J("value")]    public required string Value      { get; set; }
		[J("verified")] public          bool?  IsVerified { get; set; }
	}

	[PgName("user_profile_ffvisibility_enum")]
	public enum UserProfileFFVisibility
	{
		[PgName("public")]    Public    = 0,
		[PgName("followers")] Followers = 1,
		[PgName("private")]   Private   = 2
	}

	private class EntityTypeConfiguration : IEntityTypeConfiguration<UserProfile>
	{
		public void Configure(EntityTypeBuilder<UserProfile> entity)
		{
			entity.Property(e => e.Birthday)
			      .IsFixedLength()
			      .HasComment("The birthday (YYYY-MM-DD) of the User.");
			entity.Property(e => e.Description).HasComment("The description (bio) of the User.");
			entity.Property(e => e.Fields).HasDefaultValueSql("'[]'::jsonb");
			entity.Property(e => e.Location).HasComment("The location of the User.");
			entity.Property(e => e.Mentions).HasDefaultValueSql("'[]'::jsonb");
			entity.Property(e => e.ModerationNote).HasDefaultValueSql("''::character varying");
			entity.Property(e => e.Url).HasComment("Remote URL of the user.");
			entity.Property(e => e.UserHost).HasComment("[Denormalized]");
			entity.Property(e => e.FFVisibility)
			      .HasDefaultValue(UserProfileFFVisibility.Public);
			entity.Property(e => e.MentionsResolved).HasDefaultValue(false);

			entity.HasOne(d => d.PinnedPage)
			      .WithOne(p => p.UserProfile)
			      .OnDelete(DeleteBehavior.SetNull);

			entity.HasOne(d => d.User)
			      .WithOne(p => p.UserProfile)
			      .OnDelete(DeleteBehavior.Cascade);
		}
	}
}
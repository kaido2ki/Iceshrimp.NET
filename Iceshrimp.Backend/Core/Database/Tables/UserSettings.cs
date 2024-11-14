using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("user_settings")]
public class UserSettings
{
	[Key]
	[Column("userId")]
	[StringLength(32)]
	public string UserId { get; set; } = null!;

	[ForeignKey(nameof(UserId))]
	[InverseProperty(nameof(Tables.User.UserSettings))]
	public virtual User User { get; set; } = null!;

	[Column("defaultNoteVisibility")]   public Note.NoteVisibility DefaultNoteVisibility   { get; set; }
	[Column("defaultRenoteVisibility")] public Note.NoteVisibility DefaultRenoteVisibility { get; set; }
	[Column("privateMode")]             public bool                PrivateMode             { get; set; }
	[Column("filterInaccessible")]      public bool                FilterInaccessible      { get; set; }
	[Column("autoAcceptFollowed")]      public bool                AutoAcceptFollowed      { get; set; }
	[Column("alwaysMarkNsfw")]          public bool                AlwaysMarkSensitive     { get; set; }

	// @formatter:off

	[Column("email")]
	[StringLength(128)]
	public string? Email { get; set; }

	[Column("emailVerified")]
	public bool EmailVerified { get; set; }

	[Column("twoFactorTempSecret")]
	[StringLength(128)]
	public string? TwoFactorTempSecret { get; set; }

	[Column("twoFactorSecret")]
	[StringLength(128)]
	public string? TwoFactorSecret { get; set; }

	[Column("twoFactorEnabled")]
	public bool TwoFactorEnabled { get; set; }

	[Column("password")]
	[StringLength(128)]
	public string? Password { get; set; }

	// @formatter:on

	private class EntityTypeConfiguration : IEntityTypeConfiguration<UserSettings>
	{
		public void Configure(EntityTypeBuilder<UserSettings> entity)
		{
			entity.Property(e => e.PrivateMode).HasDefaultValue(false);
			entity.Property(e => e.FilterInaccessible).HasDefaultValue(false);
			entity.Property(e => e.DefaultNoteVisibility).HasDefaultValue(Note.NoteVisibility.Public);
			entity.Property(e => e.DefaultRenoteVisibility).HasDefaultValue(Note.NoteVisibility.Public);
			entity.Property(e => e.AlwaysMarkSensitive).HasDefaultValue(false);
			entity.Property(e => e.AutoAcceptFollowed).HasDefaultValue(false);
			entity.Property(e => e.Email);
			entity.Property(e => e.EmailVerified).HasDefaultValue(false);
			entity.Property(e => e.Password);
			entity.Property(e => e.TwoFactorEnabled).HasDefaultValue(false);
			entity.HasOne(e => e.User).WithOne(e => e.UserSettings);
		}
	}
}
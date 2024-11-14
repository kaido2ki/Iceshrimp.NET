using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("messaging_message")]
[Index(nameof(GroupId))]
[Index(nameof(UserId))]
[Index(nameof(RecipientId))]
[Index(nameof(CreatedAt))]
public class MessagingMessage
{
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the MessagingMessage.
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	/// <summary>
	///     The sender user ID.
	/// </summary>
	[Column("userId")]
	[StringLength(32)]
	public string UserId { get; set; } = null!;

	/// <summary>
	///     The recipient user ID.
	/// </summary>
	[Column("recipientId")]
	[StringLength(32)]
	public string? RecipientId { get; set; }

	[Column("text")] [StringLength(4096)] public string? Text { get; set; }

	[Column("isRead")] public bool IsRead { get; set; }

	[Column("fileId")] [StringLength(32)] public string? FileId { get; set; }

	/// <summary>
	///     The recipient group ID.
	/// </summary>
	[Column("groupId")]
	[StringLength(32)]
	public string? GroupId { get; set; }

	[Column("reads", TypeName = "character varying(32)[]")]
	public List<string> Reads { get; set; } = null!;

	[Column("uri")] [StringLength(512)] public string? Uri { get; set; }

	[ForeignKey(nameof(FileId))]
	[InverseProperty(nameof(DriveFile.MessagingMessages))]
	public virtual DriveFile? File { get; set; }

	[ForeignKey(nameof(GroupId))]
	[InverseProperty(nameof(UserGroup.MessagingMessages))]
	public virtual UserGroup? Group { get; set; }

	[ForeignKey(nameof(RecipientId))]
	[InverseProperty(nameof(Tables.User.MessagingMessageRecipients))]
	public virtual User? Recipient { get; set; }

	[ForeignKey(nameof(UserId))]
	[InverseProperty(nameof(Tables.User.MessagingMessageUsers))]
	public virtual User User { get; set; } = null!;
	
	private class EntityTypeConfiguration : IEntityTypeConfiguration<MessagingMessage>
	{
		public void Configure(EntityTypeBuilder<MessagingMessage> entity)
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the MessagingMessage.");
			entity.Property(e => e.GroupId).HasComment("The recipient group ID.");
			entity.Property(e => e.IsRead).HasDefaultValue(false);
			entity.Property(e => e.Reads).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.RecipientId).HasComment("The recipient user ID.");
			entity.Property(e => e.UserId).HasComment("The sender user ID.");

			entity.HasOne(d => d.File)
			      .WithMany(p => p.MessagingMessages)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.Group)
			      .WithMany(p => p.MessagingMessages)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.Recipient)
			      .WithMany(p => p.MessagingMessageRecipients)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.User)
			      .WithMany(p => p.MessagingMessageUsers)
			      .OnDelete(DeleteBehavior.Cascade);
		}
	}
}
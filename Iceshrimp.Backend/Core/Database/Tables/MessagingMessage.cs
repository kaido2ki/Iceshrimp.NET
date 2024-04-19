using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

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

	[ForeignKey("FileId")]
	[InverseProperty(nameof(DriveFile.MessagingMessages))]
	public virtual DriveFile? File { get; set; }

	[ForeignKey("GroupId")]
	[InverseProperty(nameof(UserGroup.MessagingMessages))]
	public virtual UserGroup? Group { get; set; }

	[ForeignKey("RecipientId")]
	[InverseProperty(nameof(Tables.User.MessagingMessageRecipients))]
	public virtual User? Recipient { get; set; }

	[ForeignKey("UserId")]
	[InverseProperty(nameof(Tables.User.MessagingMessageUsers))]
	public virtual User User { get; set; } = null!;
}
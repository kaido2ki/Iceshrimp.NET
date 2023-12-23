using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("messaging_message")]
[Index("GroupId", Name = "IDX_2c4be03b446884f9e9c502135b")]
[Index("UserId", Name = "IDX_5377c307783fce2b6d352e1203")]
[Index("RecipientId", Name = "IDX_cac14a4e3944454a5ce7daa514")]
[Index("CreatedAt", Name = "IDX_e21cd3646e52ef9c94aaf17c2e")]
public class MessagingMessage {
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
	[InverseProperty("MessagingMessages")]
	public virtual DriveFile? File { get; set; }

	[ForeignKey("GroupId")]
	[InverseProperty("MessagingMessages")]
	public virtual UserGroup? Group { get; set; }

	[ForeignKey("RecipientId")]
	[InverseProperty("MessagingMessageRecipients")]
	public virtual User? Recipient { get; set; }

	[ForeignKey("UserId")]
	[InverseProperty("MessagingMessageUsers")]
	public virtual User User { get; set; } = null!;
}
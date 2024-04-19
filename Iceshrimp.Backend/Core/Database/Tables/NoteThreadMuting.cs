using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("note_thread_muting")]
[Index(nameof(UserId))]
[Index(nameof(UserId), nameof(ThreadId), IsUnique = true)]
[Index(nameof(ThreadId))]
public class NoteThreadMuting
{
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	[Column("createdAt")] public DateTime CreatedAt { get; set; }

	[Column("userId")] [StringLength(32)] public string UserId { get; set; } = null!;

	[Column("threadId")]
	[StringLength(256)]
	public string ThreadId { get; set; } = null!;

	[ForeignKey(nameof(UserId))]
	[InverseProperty(nameof(Tables.User.NoteThreadMutings))]
	public virtual User User { get; set; } = null!;
}
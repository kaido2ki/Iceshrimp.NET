using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("note_thread_muting")]
[Index("UserId", Name = "IDX_29c11c7deb06615076f8c95b80")]
[Index("UserId", "ThreadId", Name = "IDX_ae7aab18a2641d3e5f25e0c4ea", IsUnique = true)]
[Index("ThreadId", Name = "IDX_c426394644267453e76f036926")]
public class NoteThreadMuting {
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	[Column("createdAt")] public DateTime CreatedAt { get; set; }

	[Column("userId")] [StringLength(32)] public string UserId { get; set; } = null!;

	[Column("threadId")]
	[StringLength(256)]
	public string ThreadId { get; set; } = null!;

	[ForeignKey("UserId")]
	[InverseProperty("NoteThreadMutings")]
	public virtual User User { get; set; } = null!;
}
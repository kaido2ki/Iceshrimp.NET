using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("sw_subscription")]
[Index("UserId")]
public class SwSubscription {
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	[Column("createdAt")] public DateTime CreatedAt { get; set; }

	[Column("userId")] [StringLength(32)] public string UserId { get; set; } = null!;

	[Column("endpoint")]
	[StringLength(512)]
	public string Endpoint { get; set; } = null!;

	[Column("auth")] [StringLength(256)] public string Auth { get; set; } = null!;

	[Column("publickey")]
	[StringLength(128)]
	public string Publickey { get; set; } = null!;

	[Column("sendReadMessage")] public bool SendReadMessage { get; set; }

	[ForeignKey("UserId")]
	[InverseProperty(nameof(Tables.User.SwSubscriptions))]
	public virtual User User { get; set; } = null!;
}
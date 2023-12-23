using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("signin")]
[Index("UserId", Name = "IDX_2c308dbdc50d94dc625670055f")]
public class Signin {
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the Signin.
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	[Column("userId")] [StringLength(32)] public string UserId { get; set; } = null!;

	[Column("ip")] [StringLength(128)] public string Ip { get; set; } = null!;

	[Column("headers", TypeName = "jsonb")]
	public string Headers { get; set; } = null!;

	[Column("success")] public bool Success { get; set; }

	[ForeignKey("UserId")]
	[InverseProperty("Signins")]
	public virtual User User { get; set; } = null!;
}
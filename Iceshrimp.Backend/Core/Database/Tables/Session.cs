using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("session")]
[Index("Token")]
public class Session {
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the OAuth token
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	[Column("userId")] [StringLength(32)] public string UserId { get; set; } = null!;

	/// <summary>
	///     The authorization token
	/// </summary>
	[Column("token")]
	[StringLength(64)]
	public string Token { get; set; } = null!;

	/// <summary>
	///     Whether or not the token has been activated (i.e. 2fa has been confirmed)
	/// </summary>
	[Column("active")]
	public bool Active { get; set; }

	[ForeignKey("UserId")]
	[InverseProperty("Sessions")]
	public virtual User User { get; set; } = null!;
}
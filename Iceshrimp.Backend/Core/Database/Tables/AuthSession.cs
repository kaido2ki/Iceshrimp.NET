using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("auth_session")]
[Index("Token", Name = "IDX_62cb09e1129f6ec024ef66e183")]
public class AuthSession {
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the AuthSession.
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	[Column("token")] [StringLength(128)] public string Token { get; set; } = null!;

	[Column("userId")] [StringLength(32)] public string? UserId { get; set; }

	[Column("appId")] [StringLength(32)] public string AppId { get; set; } = null!;

	[ForeignKey("AppId")]
	[InverseProperty("AuthSessions")]
	public virtual App App { get; set; } = null!;

	[ForeignKey("UserId")]
	[InverseProperty("AuthSessions")]
	public virtual User? User { get; set; }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("app")]
[Index("CreatedAt", Name = "IDX_048a757923ed8b157e9895da53")]
[Index("UserId", Name = "IDX_3f5b0899ef90527a3462d7c2cb")]
[Index("Secret", Name = "IDX_f49922d511d666848f250663c4")]
public class App {
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the App.
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	/// <summary>
	///     The owner ID.
	/// </summary>
	[Column("userId")]
	[StringLength(32)]
	public string? UserId { get; set; }

	/// <summary>
	///     The secret key of the App.
	/// </summary>
	[Column("secret")]
	[StringLength(64)]
	public string Secret { get; set; } = null!;

	/// <summary>
	///     The name of the App.
	/// </summary>
	[Column("name")]
	[StringLength(128)]
	public string Name { get; set; } = null!;

	/// <summary>
	///     The description of the App.
	/// </summary>
	[Column("description")]
	[StringLength(512)]
	public string Description { get; set; } = null!;

	/// <summary>
	///     The permission of the App.
	/// </summary>
	[Column("permission", TypeName = "character varying(64)[]")]
	public List<string> Permission { get; set; } = null!;

	/// <summary>
	///     The callbackUrl of the App.
	/// </summary>
	[Column("callbackUrl")]
	[StringLength(512)]
	public string? CallbackUrl { get; set; }

	[InverseProperty("App")]
	public virtual ICollection<AccessToken> AccessTokens { get; set; } = new List<AccessToken>();

	[InverseProperty("App")]
	public virtual ICollection<AuthSession> AuthSessions { get; set; } = new List<AuthSession>();

	[ForeignKey("UserId")]
	[InverseProperty("Apps")]
	public virtual User? User { get; set; }
}
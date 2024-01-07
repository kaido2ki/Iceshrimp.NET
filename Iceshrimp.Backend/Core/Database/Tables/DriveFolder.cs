using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("drive_folder")]
[Index("ParentId")]
[Index("CreatedAt")]
[Index("UserId")]
public class DriveFolder {
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the DriveFolder.
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	/// <summary>
	///     The name of the DriveFolder.
	/// </summary>
	[Column("name")]
	[StringLength(128)]
	public string Name { get; set; } = null!;

	/// <summary>
	///     The owner ID.
	/// </summary>
	[Column("userId")]
	[StringLength(32)]
	public string? UserId { get; set; }

	/// <summary>
	///     The parent folder ID. If null, it means the DriveFolder is located in root.
	/// </summary>
	[Column("parentId")]
	[StringLength(32)]
	public string? ParentId { get; set; }

	[InverseProperty("Folder")] public virtual ICollection<DriveFile> DriveFiles { get; set; } = new List<DriveFile>();

	[InverseProperty("Parent")]
	public virtual ICollection<DriveFolder> InverseParent { get; set; } = new List<DriveFolder>();

	[ForeignKey("ParentId")]
	[InverseProperty("InverseParent")]
	public virtual DriveFolder? Parent { get; set; }

	[ForeignKey("UserId")]
	[InverseProperty("DriveFolders")]
	public virtual User? User { get; set; }
}
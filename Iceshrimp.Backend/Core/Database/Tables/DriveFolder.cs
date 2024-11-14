using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("drive_folder")]
[Index(nameof(ParentId))]
[Index(nameof(CreatedAt))]
[Index(nameof(UserId))]
public class DriveFolder
{
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

	[InverseProperty(nameof(DriveFile.Folder))]
	public virtual ICollection<DriveFile> DriveFiles { get; set; } = new List<DriveFile>();

	[InverseProperty(nameof(Parent))]
	public virtual ICollection<DriveFolder> InverseParent { get; set; } = new List<DriveFolder>();

	[ForeignKey(nameof(ParentId))]
	[InverseProperty(nameof(InverseParent))]
	public virtual DriveFolder? Parent { get; set; }

	[ForeignKey(nameof(UserId))]
	[InverseProperty(nameof(Tables.User.DriveFolders))]
	public virtual User? User { get; set; }
	
	private class EntityTypeConfiguration : IEntityTypeConfiguration<DriveFolder>
	{
		public void Configure(EntityTypeBuilder<DriveFolder> entity)
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the DriveFolder.");
			entity.Property(e => e.Name).HasComment("The name of the DriveFolder.");
			entity.Property(e => e.ParentId)
			      .HasComment("The parent folder ID. If null, it means the DriveFolder is located in root.");
			entity.Property(e => e.UserId).HasComment("The owner ID.");

			entity.HasOne(d => d.Parent)
			      .WithMany(p => p.InverseParent)
			      .OnDelete(DeleteBehavior.SetNull);

			entity.HasOne(d => d.User)
			      .WithMany(p => p.DriveFolders)
			      .OnDelete(DeleteBehavior.Cascade);
		}
	}
}
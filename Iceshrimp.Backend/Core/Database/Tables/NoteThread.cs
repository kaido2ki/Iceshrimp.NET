using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using EntityFrameworkCore.Projectables;
using Iceshrimp.Backend.Core.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("note_thread")]
[Index(nameof(Uri), IsUnique = true)]
public class NoteThread : IEntity
{
	[Column("id")]
	[StringLength(256)]
	public required string Id { get; set; }

	/// <summary>
	///     The last time this thread has been backfilled.
	/// </summary>
	[Column("backfilledAt")]
	public DateTime? BackfilledAt { get; set; }

	/// <summary>
	///     The ID of the collection representing this thread. Will be null when the thread not part of a context collection.
	/// </summary>
	[Column("uri")]
	[StringLength(512)]
	public string? Uri { get; set; }

	/// <summary>
	///     The User owning this thread. Will be null if unknown. Determined by the context collection's attributedTo property.
	/// </summary>
	[Column("userId")]
	[StringLength(32)]
	public string? UserId { get; set; }

	/// <summary>
	///     Is the context collection associated with this thread resolvable? Null if this is a local thread that we don't need to resolve.
	/// </summary>
	[Column("isResolvable")]
	public bool? IsResolvable { get; set; } = false;

	[InverseProperty(nameof(Note.Thread))]
	public virtual ICollection<Note> Notes { get; set; } = new List<Note>();

	[InverseProperty(nameof(NoteThreadMuting.Thread))]
	public virtual ICollection<NoteThreadMuting> NoteThreadMutings { get; set; } = new List<NoteThreadMuting>();

	[ForeignKey(nameof(UserId))]
	public virtual User? User { get; set; }

	[Projectable]
	[SuppressMessage("ReSharper", "MergeIntoPattern", Justification = "Projectables does not support this")]
	public string? GetPublicUri(Config.InstanceSection config) => User != null && User.IsLocalUser ? $"https://{config.WebDomain}/threads/{Id}" : null;
	
	private class NoteThreadConfiguration : IEntityTypeConfiguration<NoteThread>
	{
		public void Configure(EntityTypeBuilder<NoteThread> entity)
		{
			entity.HasOne(e => e.User)
			      .WithMany()
			      .OnDelete(DeleteBehavior.SetNull);
		}
	}
}

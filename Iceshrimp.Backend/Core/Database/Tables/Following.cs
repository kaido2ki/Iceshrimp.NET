using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("following")]
[Index(nameof(FolloweeId))]
[Index(nameof(FollowerId), nameof(FolloweeId), IsUnique = true)]
[Index(nameof(FollowerHost))]
[Index(nameof(CreatedAt))]
[Index(nameof(FollowerId))]
[Index(nameof(FolloweeHost))]
public class Following
{
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the Following.
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	/// <summary>
	///     The followee user ID.
	/// </summary>
	[Column("followeeId")]
	[StringLength(32)]
	public string FolloweeId { get; set; } = null!;

	/// <summary>
	///     The follower user ID.
	/// </summary>
	[Column("followerId")]
	[StringLength(32)]
	public string FollowerId { get; set; } = null!;

	/// <summary>
	///     [Denormalized]
	/// </summary>
	[Column("followerHost")]
	[StringLength(512)]
	public string? FollowerHost { get; set; }

	/// <summary>
	///     [Denormalized]
	/// </summary>
	[Column("followerInbox")]
	[StringLength(512)]
	public string? FollowerInbox { get; set; }

	/// <summary>
	///     [Denormalized]
	/// </summary>
	[Column("followerSharedInbox")]
	[StringLength(512)]
	public string? FollowerSharedInbox { get; set; }

	/// <summary>
	///     [Denormalized]
	/// </summary>
	[Column("followeeHost")]
	[StringLength(512)]
	public string? FolloweeHost { get; set; }

	/// <summary>
	///     [Denormalized]
	/// </summary>
	[Column("followeeInbox")]
	[StringLength(512)]
	public string? FolloweeInbox { get; set; }

	/// <summary>
	///     [Denormalized]
	/// </summary>
	[Column("followeeSharedInbox")]
	[StringLength(512)]
	public string? FolloweeSharedInbox { get; set; }

	[Column("relationshipId")] public Guid? RelationshipId { get; set; }

	[ForeignKey(nameof(FolloweeId))]
	[InverseProperty(nameof(User.IncomingFollowRelationships))]
	public virtual User Followee { get; set; } = null!;

	[ForeignKey(nameof(FollowerId))]
	[InverseProperty(nameof(User.OutgoingFollowRelationships))]
	public virtual User Follower { get; set; } = null!;
	
	private class EntityTypeConfiguration : IEntityTypeConfiguration<Following>
	{
		public void Configure(EntityTypeBuilder<Following> entity)
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the Following.");
			entity.Property(e => e.FolloweeHost).HasComment("[Denormalized]");
			entity.Property(e => e.FolloweeId).HasComment("The followee user ID.");
			entity.Property(e => e.FolloweeInbox).HasComment("[Denormalized]");
			entity.Property(e => e.FolloweeSharedInbox).HasComment("[Denormalized]");
			entity.Property(e => e.FollowerHost).HasComment("[Denormalized]");
			entity.Property(e => e.FollowerId).HasComment("The follower user ID.");
			entity.Property(e => e.FollowerInbox).HasComment("[Denormalized]");
			entity.Property(e => e.FollowerSharedInbox).HasComment("[Denormalized]");

			entity.HasOne(d => d.Followee)
			      .WithMany(p => p.IncomingFollowRelationships)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.Follower)
			      .WithMany(p => p.OutgoingFollowRelationships)
			      .OnDelete(DeleteBehavior.Cascade);
		}
	}
}
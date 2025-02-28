﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("follow_request")]
[Index(nameof(FolloweeId))]
[Index(nameof(FollowerId))]
[Index(nameof(FollowerId), nameof(FolloweeId), IsUnique = true)]
public class FollowRequest : IEntity
{
	/// <summary>
	///     The created date of the FollowRequest.
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
	///     id of Follow Activity.
	/// </summary>
	[Column("requestId")]
	[StringLength(512)]
	public string? RequestId { get; set; }

	[Column("relationshipId")] public Guid? RelationshipId { get; set; }

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

	[ForeignKey(nameof(FolloweeId))]
	[InverseProperty(nameof(User.IncomingFollowRequests))]
	public virtual User Followee { get; set; } = null!;

	[ForeignKey(nameof(FollowerId))]
	[InverseProperty(nameof(User.OutgoingFollowRequests))]
	public virtual User Follower { get; set; } = null!;

	[InverseProperty(nameof(Notification.FollowRequest))]
	public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;
	
	private class EntityTypeConfiguration : IEntityTypeConfiguration<FollowRequest>
	{
		public void Configure(EntityTypeBuilder<FollowRequest> entity)
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the FollowRequest.");
			entity.Property(e => e.FolloweeHost).HasComment("[Denormalized]");
			entity.Property(e => e.FolloweeId).HasComment("The followee user ID.");
			entity.Property(e => e.FolloweeInbox).HasComment("[Denormalized]");
			entity.Property(e => e.FolloweeSharedInbox).HasComment("[Denormalized]");
			entity.Property(e => e.FollowerHost).HasComment("[Denormalized]");
			entity.Property(e => e.FollowerId).HasComment("The follower user ID.");
			entity.Property(e => e.FollowerInbox).HasComment("[Denormalized]");
			entity.Property(e => e.FollowerSharedInbox).HasComment("[Denormalized]");
			entity.Property(e => e.RequestId).HasComment("id of Follow Activity.");

			entity.HasOne(d => d.Followee)
			      .WithMany(p => p.IncomingFollowRequests)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.Follower)
			      .WithMany(p => p.OutgoingFollowRequests)
			      .OnDelete(DeleteBehavior.Cascade);
		}
	}
}
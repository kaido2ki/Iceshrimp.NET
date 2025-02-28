﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("push_subscription")]
[Index(nameof(UserId))]
[Index(nameof(OauthTokenId), IsUnique = true)]
public class PushSubscription
{
	[PgName("push_subscription_policy_enum")]
	public enum PushPolicy
	{
		[PgName("all")]      All,
		[PgName("followed")] Followed,
		[PgName("follower")] Follower,
		[PgName("none")]     None
	}

	[Column("types", TypeName = "character varying(32)[]")]
	public List<string> Types { get; set; } = null!;

	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	[Column("createdAt")] public DateTime CreatedAt { get; set; }

	[Column("userId")] [StringLength(32)] public string UserId { get; set; } = null!;

	[Column("oauthTokenId")]
	[StringLength(32)]
	public string OauthTokenId { get; set; } = null!;

	[Column("endpoint")]
	[StringLength(512)]
	public string Endpoint { get; set; } = null!;

	[Column("auth")] [StringLength(256)] public string AuthSecret { get; set; } = null!;

	[Column("publickey")]
	[StringLength(128)]
	public string PublicKey { get; set; } = null!;

	[Column("policy")] public PushPolicy Policy { get; set; }

	[ForeignKey(nameof(UserId))]
	[InverseProperty(nameof(Tables.User.PushSubscriptions))]
	public virtual User User { get; set; } = null!;

	[ForeignKey(nameof(OauthTokenId))]
	[InverseProperty(nameof(Tables.OauthToken.PushSubscription))]
	public virtual OauthToken OauthToken { get; set; } = null!;

	private class EntityTypeConfiguration : IEntityTypeConfiguration<PushSubscription>
	{
		public void Configure(EntityTypeBuilder<PushSubscription> entity)
		{
			entity.Property(e => e.Types).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.Policy).HasDefaultValue(PushPolicy.All);

			entity.HasOne(d => d.User)
			      .WithMany(p => p.PushSubscriptions)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.OauthToken)
			      .WithOne(p => p.PushSubscription)
			      .OnDelete(DeleteBehavior.Cascade);
		}
	}
}
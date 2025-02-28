﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[PrimaryKey(nameof(Id), nameof(UserId))]
[Table("attestation_challenge")]
[Index(nameof(Challenge))]
[Index(nameof(UserId))]
public class AttestationChallenge
{
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	[Key]
	[Column("userId")]
	[StringLength(32)]
	public string UserId { get; set; } = null!;

	/// <summary>
	///     Hex-encoded sha256 hash of the challenge.
	/// </summary>
	[Column("challenge")]
	[StringLength(64)]
	public string Challenge { get; set; } = null!;

	/// <summary>
	///     The date challenge was created for expiry purposes.
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	/// <summary>
	///     Indicates that the challenge is only for registration purposes if true to prevent the challenge for being used as
	///     authentication.
	/// </summary>
	[Column("registrationChallenge")]
	public bool RegistrationChallenge { get; set; }

	[ForeignKey(nameof(UserId))]
	[InverseProperty(nameof(Tables.User.AttestationChallenges))]
	public virtual User User { get; set; } = null!;
	
	private class EntityTypeConfiguration : IEntityTypeConfiguration<AttestationChallenge>
	{
		public void Configure(EntityTypeBuilder<AttestationChallenge> entity)
		{
			entity.Property(e => e.Challenge).HasComment("Hex-encoded sha256 hash of the challenge.");
			entity.Property(e => e.CreatedAt).HasComment("The date challenge was created for expiry purposes.");
			entity.Property(e => e.RegistrationChallenge)
			      .HasDefaultValue(false)
			      .HasComment("Indicates that the challenge is only for registration purposes if true to prevent the challenge for being used as authentication.");

			entity.HasOne(d => d.User)
			      .WithMany(p => p.AttestationChallenges)
			      .OnDelete(DeleteBehavior.Cascade);
		}
	}
}
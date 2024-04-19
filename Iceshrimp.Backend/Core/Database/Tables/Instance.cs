using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EntityFrameworkCore.Projectables;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("instance")]
[Index(nameof(CaughtAt))]
[Index(nameof(IsSuspended))]
[Index(nameof(Host), IsUnique = true)]
public class Instance
{
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The caught date of the Instance.
	/// </summary>
	[Column("caughtAt")]
	public DateTime CaughtAt { get; set; }

	/// <summary>
	///     The host of the Instance.
	/// </summary>
	[Column("host")]
	[StringLength(512)]
	public string Host { get; set; } = null!;

	/// <summary>
	///     The count of the users of the Instance.
	/// </summary>
	[Column("usersCount")]
	public int UsersCount { get; set; }

	/// <summary>
	///     The count of the notes of the Instance.
	/// </summary>
	[Column("notesCount")]
	public int NotesCount { get; set; }

	[Column("incomingFollows")] public int IncomingFollows { get; set; }

	[Column("outgoingFollows")] public int OutgoingFollows { get; set; }

	[Column("latestRequestSentAt")] public DateTime? LatestRequestSentAt { get; set; }

	[Column("latestStatus")] public int? LatestStatus { get; set; }

	[Column("latestRequestReceivedAt")] public DateTime? LatestRequestReceivedAt { get; set; }

	[Column("lastCommunicatedAt")] public DateTime LastCommunicatedAt { get; set; }

	[Column("isNotResponding")] public bool IsNotResponding { get; set; }

	/// <summary>
	///     The software of the Instance.
	/// </summary>
	[Column("softwareName")]
	[StringLength(64)]
	public string? SoftwareName { get; set; }

	[Column("softwareVersion")]
	[StringLength(64)]
	public string? SoftwareVersion { get; set; }

	[Column("openRegistrations")] public bool? OpenRegistrations { get; set; }

	[Column("name")] [StringLength(256)] public string? Name { get; set; }

	[Column("description")]
	[StringLength(4096)]
	public string? Description { get; set; }

	[Column("maintainerName")]
	[StringLength(128)]
	public string? MaintainerName { get; set; }

	[Column("maintainerEmail")]
	[StringLength(256)]
	public string? MaintainerEmail { get; set; }

	[Column("infoUpdatedAt")] public DateTime? InfoUpdatedAt { get; set; }

	[Column("isSuspended")] public bool IsSuspended { get; set; }

	[Column("iconUrl")]
	[StringLength(4096)]
	public string? IconUrl { get; set; }

	[Column("themeColor")]
	[StringLength(64)]
	public string? ThemeColor { get; set; }

	[Column("faviconUrl")]
	[StringLength(4096)]
	public string? FaviconUrl { get; set; }

	[NotMapped]
	[Projectable]
	public bool NeedsUpdate => InfoUpdatedAt == null || InfoUpdatedAt < DateTime.Now - TimeSpan.FromHours(24);
}
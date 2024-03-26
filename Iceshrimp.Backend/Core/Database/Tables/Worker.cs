using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("worker")]
[Index("Id")]
[Index("Heartbeat")]
public class Worker
{
	[Key]
	[Column("id")]
	[StringLength(64)]
	public string Id { get; set; } = null!;

	[Column("heartbeat")] public DateTime Heartbeat { get; set; }
}
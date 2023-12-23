using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("registration_ticket")]
[Index("Code", Name = "IDX_0ff69e8dfa9fe31bb4a4660f59", IsUnique = true)]
public class RegistrationTicket {
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	[Column("createdAt")] public DateTime CreatedAt { get; set; }

	[Column("code")] [StringLength(64)] public string Code { get; set; } = null!;
}
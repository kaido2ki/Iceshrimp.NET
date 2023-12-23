using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("user_ip")]
[Index("UserId", "Ip", Name = "IDX_361b500e06721013c124b7b6c5", IsUnique = true)]
[Index("UserId", Name = "IDX_7f7f1c66f48e9a8e18a33bc515")]
public class UserIp {
	[Key] [Column("id")] public int Id { get; set; }

	[Column("createdAt")] public DateTime CreatedAt { get; set; }

	[Column("userId")] [StringLength(32)] public string UserId { get; set; } = null!;

	[Column("ip")] [StringLength(128)] public string Ip { get; set; } = null!;
}
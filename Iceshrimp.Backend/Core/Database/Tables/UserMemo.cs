using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[PrimaryKey(nameof(ByUserId), nameof(TargetUserId))]
[Table("user_memo")]
public class UserMemo
{
	[Column("by_user_id")]
	[StringLength(32)]
	public string ByUserId { get; set; } = null!;
	
	[Column("target_user_id")]
	[StringLength(32)]
	public string TargetUserId { get; set; } = null!;
	
	[Column("text")]
	[StringLength(100000)]
	public string Text { get; set; } = null!;
}

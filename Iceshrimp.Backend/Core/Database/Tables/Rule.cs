using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("rule")]
public class Rule
{
    [Key]
    [Column("id")]
    [StringLength(32)]
    public string Id { get; set; } = null!;

    [Column("order")]
    public int Order { get; set; }

    [Column("text")]
    [StringLength(128)]
    public string Text { get; set; } = null!;

    [Column("description")]
    [StringLength(512)]
    public string? Description { get; set; }
}

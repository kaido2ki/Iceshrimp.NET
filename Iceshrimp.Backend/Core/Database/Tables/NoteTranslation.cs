using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[PrimaryKey(nameof(NoteId), nameof(TargetLanguage))]
[Table("note_translation")]
public class NoteTranslation
{
    [Key]
    [Column("noteId")]
    [StringLength(32)]
    public string NoteId { get; set; } = null!;
    
    [Column("noteEditId")]
    [StringLength(32)]
    public string? NoteEditId { get; set; }

    [Column("text")]
    public string? Text { get; set; }
    
    [Column("cw")]
    public string? Cw { get; set; }
    
    [Column("originalLanguage")]
    public string OriginalLanguage { get; set; } = null!;
    
    [Key]
    [Column("targetLanguage")]
    public string TargetLanguage { get; set; } = null!;
    
    [Column("pollChoices", TypeName = "character varying(256)[]")]
    public List<string>? PollChoices { get; set; }
}
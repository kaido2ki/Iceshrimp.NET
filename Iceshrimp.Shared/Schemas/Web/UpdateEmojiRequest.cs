namespace Iceshrimp.Shared.Schemas.Web;

public class UpdateEmojiRequest
{
    public string?       Name      { get; set; }
    public List<string>? Tags      { get; set; }
    public string?       Category  { get; set; }
    public string?       License   { get; set; }
    public bool?         Sensitive { get; set; }
}

public class BatchUpdateEmojiRequest
{
    public required List<string> Ids       { get; set; }
    public          string?      Category  { get; set; }
    public          string?      License   { get; set; }
    public          bool?        Sensitive { get; set; }
}
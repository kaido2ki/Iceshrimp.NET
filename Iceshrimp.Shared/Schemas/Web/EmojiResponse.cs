namespace Iceshrimp.Shared.Schemas.Web;

public class EmojiResponse
{
	public required string       Id        { get; set; }
	public required string       Name      { get; set; }
	public required string?      Uri       { get; set; }
	public required List<string> Aliases   { get; set; }
	public required string?      Category  { get; set; }
	public required string       PublicUrl { get; set; }
	public required string?      License   { get; set; }
}
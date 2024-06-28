namespace Iceshrimp.Shared.Schemas;

public class UpdateEmojiRequest
{
	public string?       Name     { get; set; }
	public List<string>? Aliases  { get; set; }
	public string?       Category { get; set; }
	public string?       License  { get; set; }
}
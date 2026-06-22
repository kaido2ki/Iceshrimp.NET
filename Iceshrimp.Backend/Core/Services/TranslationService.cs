using Iceshrimp.Backend.Core.Database.Tables;

namespace Iceshrimp.Backend.Core.Services;

public interface ITranslationService
{
	Task<Translation> TranslateAsync(string text, string targetLanguage);
	Task<Translation> TranslateAllFieldsAsync(Note note, string targetLanguage);
	
	public class Translation
	{
		public required string        TranslatedText   { get; set; }
		public          string?       TranslatedCw     { get; set; }
		public          List<string>? TranslatedPoll   { get; set; }
		public required string        OriginalLanguage { get; set; }
	}
}
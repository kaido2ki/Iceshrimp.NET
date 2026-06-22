namespace Iceshrimp.Backend.Core.Services;

public interface ITranslationService
{
	Task<Translation> TranslateAsync(string text);
	
	public class Translation
	{
		public required string TranslatedText   { get; set; }
		public required string DetectedLanguage { get; set; }
	}
}
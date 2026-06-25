using System.Globalization;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Utils.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Services;

public class TranslationService(
	DatabaseContext db,
	IServiceProvider provider
) : IScopedService
{
	public async Task<(ITranslationProvider.Translation, string outputLanguage)> TranslateAsync(Note note, string? targetLanguage)
	{
		var lang = CultureInfo.GetCultureInfo(targetLanguage ?? "en").ToString();
		
		var existing = await db.NoteTranslations
		                       .Where(p => p.NoteId == note.Id
		                                   && p.TargetLanguage == lang
		                                   && p.NoteEditId == null)
		                       .Select(p => new ITranslationProvider.Translation
		                       {
			                       TranslatedText   = p.Text ?? "",
			                       TranslatedCw     = p.Cw ?? "",
			                       OriginalLanguage = p.OriginalLanguage,
			                       TranslatedPoll   = p.PollChoices
		                       })
		                       .FirstOrDefaultAsync();
		if (existing != null) return (existing, lang);
		
		var translationProvider = provider.GetService<ITranslationProvider>()
		                          ?? throw GracefulException.UnprocessableEntity("No translation plugins have been set up");

		return (await translationProvider.TranslateAsync(note, lang)
		                  ?? throw GracefulException.UnprocessableEntity("There was an issue translating this note"), lang);
	}

	public string GetProviderName()
	{
		var translationProvider = provider.GetService<ITranslationProvider>()
		                     ?? throw GracefulException.UnprocessableEntity("No translation plugins have been set up");

		return translationProvider.GetProviderName();
	}
}

public interface ITranslationProvider
{
	Task<Translation> TranslateAsync(Note note, string targetLanguage);
	string            GetProviderName();
	
	public class Translation
	{
		public required string        TranslatedText   { get; set; }
		public          string?       TranslatedCw     { get; set; }
		public          List<string>? TranslatedPoll   { get; set; }
		public required string        OriginalLanguage { get; set; }
	}
}
using System.Globalization;
using AsyncKeyedLock;
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
	private static readonly AsyncKeyedLocker<string> KeyedLocker = new(o =>
	{
		o.PoolSize        = 10;
		o.PoolInitialFill = 2;
	});
	
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

		using (await KeyedLocker.LockAsync($"translations:{note.Id}:{targetLanguage}"))
		{
			var translationProvider = provider.GetService<ITranslationProvider>()
			                          ?? throw GracefulException.UnprocessableEntity("No translation plugins have been set up");

			var translation = await translationProvider.TranslateAsync(note, lang)
			        ?? throw GracefulException.UnprocessableEntity("There was an issue translating this note");
			
			db.Add(new NoteTranslation
			{
				NoteId           = note.Id,
				NoteEditId       = null,
				Text             = translation.TranslatedText,
				Cw               = translation.TranslatedCw,
				OriginalLanguage = translation.OriginalLanguage,
				TargetLanguage   = lang,
				PollChoices      = translation.TranslatedPoll,
				Provider         = translationProvider.GetProviderName()
			});
			await db.SaveChangesAsync();

			return (translation, lang);
		}
	}
	
	public async Task RemoveTranslationsAsync(Note note)
	{
		await db.NoteTranslations
		        .Where(p => p.NoteId == note.Id)
		        .ExecuteDeleteAsync();
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
using System.Globalization;
using Blazored.LocalStorage;
using Iceshrimp.Frontend.Core.Schemas;

namespace Iceshrimp.Frontend.Core.Miscellaneous;

public class LocaleHelper(ISyncLocalStorageService localStorage)

{
    public static (CultureInfo Culture, string DisplayName)[] AvailableCultures { get; } =
    [
        // Add supported languages here
        (new CultureInfo("en-150"), "English"), 
        (new CultureInfo("fr"), "Français"),
    ];
    
    private static CultureInfo ResolveCulture(string language)
    {
        if (language == "followBrowser")
            return CultureInfo.CurrentUICulture;

        try
        {
            return new CultureInfo(language);
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.CurrentUICulture;
        }
    }

    private static bool IsValidLanguage(string language) =>
        language == "followBrowser" || AvailableCultures.Any(c => c.Culture.Name == language);

    public CultureInfo LoadCulture()
    {
        var language = localStorage.GetItem<ClientPreferences>("preferences")?.Language ?? "followBrowser";
        return IsValidLanguage(language) ? ResolveCulture(language) : CultureInfo.CurrentUICulture;
    }
}

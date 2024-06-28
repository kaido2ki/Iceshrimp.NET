using System.Globalization;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;

namespace Iceshrimp.Frontend.Core.Miscellaneous;

public class LocaleHelper(ISyncLocalStorageService localStorage)
{
	[Inject] public ISyncLocalStorageService LocalStorage { get; } = localStorage;

	public CultureInfo LoadCulture()
	{
		var defaultCulture = "en-150";
		var culture        = LocalStorage.GetItem<string?>("blazorCulture") ?? defaultCulture;
		var res            = new CultureInfo(culture);
		return res;
	}

	public void StoreCulture(CultureInfo cultureInfo)
	{
		var cultureString = cultureInfo.Name;
		LocalStorage.SetItem("blazorCulture", cultureString);
	}
}
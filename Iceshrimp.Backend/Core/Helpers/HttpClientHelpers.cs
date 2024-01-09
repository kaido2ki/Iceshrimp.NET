using System.Net.Http.Headers;

namespace Iceshrimp.Backend.Core.Helpers;

public static class HttpClientHelpers {
	public static readonly HttpClient HttpClient = new() {
		DefaultRequestHeaders = {
			UserAgent = { ProductInfoHeaderValue.Parse("Iceshrimp.NET/0.0.1") }
		} //FIXME (instance domain comment in parentheses doesn't work?)
	};
}
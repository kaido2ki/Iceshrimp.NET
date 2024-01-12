using System.Net.Http.Headers;

namespace Iceshrimp.Backend.Core.Helpers;

public static class HttpClientHelpers {
	//TODO: replace with HttpClient service
	public static readonly HttpClient HttpClient = new() {
		DefaultRequestHeaders = {
			UserAgent = { ProductInfoHeaderValue.Parse("Iceshrimp.NET/0.0.1") }
		}
	};
}
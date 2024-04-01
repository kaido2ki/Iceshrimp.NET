using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Shared.Schemas;
using Microsoft.AspNetCore.Http;

namespace Iceshrimp.Frontend.Core.Extensions;

internal static class HttpClientExtensions
{
	public static async Task<T> Call<T>(
		this HttpClient client, HttpMethod method, string path, QueryString? query = null, string? body = null
	) where T : class
	{
		var res = await CallInternal<T>(client, method, path, query, body);
		if (res.result != null)
			return res.result;
		throw new ApiException(res.error ?? throw new Exception("Deserialized API error was null"));
	}

	public static async Task<T?> CallNullable<T>(
		this HttpClient client, HttpMethod method, string path, QueryString? query = null, string? body = null
	) where T : class
	{
		var res = await CallInternal<T>(client, method, path, query, body);
		if (res.result != null)
			return res.result;

		var err = res.error ?? throw new Exception("Deserialized API error was null");
		if (err.StatusCode == 404)
			return null;

		throw new ApiException(err);
	}

	private static async Task<(T? result, ErrorResponse? error)> CallInternal<T>(
		this HttpClient client, HttpMethod method, string path, QueryString? query = null, string? body = null
	) where T : class
	{
		var request = new HttpRequestMessage(method, "/api/iceshrimp/" + path.TrimStart('/') + query);
		request.Headers.Accept.ParseAdd(MediaTypeNames.Application.Json);
		if (body != null) request.Content = new StringContent(body, Encoding.UTF8, MediaTypeNames.Application.Json);

		var res = await client.SendAsync(request);
		if (res.IsSuccessStatusCode)
		{
			var deserialized = await res.Content.ReadFromJsonAsync<T>();
			if (deserialized == null)
				throw new Exception("Deserialized API response was null");
			return (deserialized, null);
		}

		var error = await res.Content.ReadFromJsonAsync<ErrorResponse>();
		if (error == null)
			throw new Exception("Deserialized API error was null");
		return (null, error);
	}
}
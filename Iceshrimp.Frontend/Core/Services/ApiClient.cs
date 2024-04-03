using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Shared.Schemas;
using Microsoft.AspNetCore.Http;

namespace Iceshrimp.Frontend.Core.Services;

internal class ApiClient(HttpClient client)
{
	private string? _token;

	public void SetToken(string token) => _token = token;

	public async Task Call(HttpMethod method, string path, QueryString? query = null, object? data = null)
	{
		var res = await MakeRequest(method, path, query, data);
		if (res.IsSuccessStatusCode)
			return;

		var error = await res.Content.ReadFromJsonAsync<ErrorResponse>();
		if (error == null)
			throw new Exception("Deserialized API error was null");
		throw new ApiException(error);
	}

	public async Task<T> Call<T>(
		HttpMethod method, string path, QueryString? query = null, object? data = null
	) where T : class
	{
		var res = await CallInternal<T>(method, path, query, data);
		if (res.result != null)
			return res.result;
		throw new ApiException(res.error ?? throw new Exception("Deserialized API error was null"));
	}

	public async Task<T?> CallNullable<T>(
		HttpMethod method, string path, QueryString? query = null, object? data = null
	) where T : class
	{
		var res = await CallInternal<T>(method, path, query, data);
		if (res.result != null)
			return res.result;

		var err = res.error ?? throw new Exception("Deserialized API error was null");
		if (err.StatusCode == 404)
			return null;

		throw new ApiException(err);
	}

	private async Task<(T? result, ErrorResponse? error)> CallInternal<T>(
		HttpMethod method, string path, QueryString? query, object? data
	) where T : class
	{
		var res = await MakeRequest(method, path, query, data);

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

	private async Task<HttpResponseMessage> MakeRequest(
		HttpMethod method, string path, QueryString? query, object? data
	)
	{
		var body    = data != null ? JsonSerializer.Serialize(data) : null;
		var request = new HttpRequestMessage(method, "/api/iceshrimp/" + path.TrimStart('/') + query);
		request.Headers.Accept.ParseAdd(MediaTypeNames.Application.Json);
		if (_token != null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
		if (body != null) request.Content = new StringContent(body, Encoding.UTF8, MediaTypeNames.Application.Json);

		return await client.SendAsync(request);
	}
}
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Extensions;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Services;

public class CustomHttpClient : HttpClient
{
	private static readonly HttpMessageHandler InnerHandler = new SocketsHttpHandler
	{
		AutomaticDecompression      = DecompressionMethods.All,
		ConnectCallback             = new FastFallback().ConnectCallback,
		PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
		PooledConnectionLifetime    = TimeSpan.FromMinutes(60)
	};

	private static readonly HttpMessageHandler Handler = new RedirectHandler(InnerHandler);

	public CustomHttpClient(IOptions<Config.InstanceSection> options) : base(Handler)
	{
		DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", options.Value.UserAgent);
		Timeout = TimeSpan.FromSeconds(30);
	}

	// Adapted from https://github.com/KazWolfe/Dalamud/blob/767cc49ecb80e29dbdda2fa8329d3c3341c964fe/Dalamud/Networking/Http/HappyEyeballsCallback.cs
	private class FastFallback(int connectionBackoff = 75)
	{
		public async ValueTask<Stream> ConnectCallback(SocketsHttpConnectionContext context, CancellationToken token)
		{
			var sortedRecords = await GetSortedAddresses(context.DnsEndPoint.Host, token);

			var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(token);
			var tasks       = new List<Task<(NetworkStream? stream, Exception? exception)>>();

			var delayCts = CancellationTokenSource.CreateLinkedTokenSource(linkedToken.Token);
			for (var i = 0; i < sortedRecords.Count; i++)
			{
				var record = sortedRecords[i];

				delayCts.CancelAfter(connectionBackoff * i);

				var task = AttemptConnection(record, context.DnsEndPoint.Port, linkedToken.Token, delayCts.Token);
				tasks.Add(task);

				var nextDelayCts = CancellationTokenSource.CreateLinkedTokenSource(linkedToken.Token);
				_        = task.ContinueWith(_ => { nextDelayCts.Cancel(); }, TaskContinuationOptions.OnlyOnFaulted);
				delayCts = nextDelayCts;
			}

			NetworkStream? stream        = null;
			Exception?     lastException = null;

			while (tasks.Count > 0 && stream == null)
			{
				var task = await Task.WhenAny(tasks).ConfigureAwait(false);
				var res  = await task;
				tasks.Remove(task);
				stream        = res.stream;
				lastException = res.exception;
			}

			if (stream == null)
			{
				throw lastException ??
				      new Exception("An unknown exception occured during fast fallback connection attempt");
			}

			await linkedToken.CancelAsync();
			tasks.ForEach(task => { task.ContinueWith(_ => Task.CompletedTask, CancellationToken.None); });

			return stream;
		}

		private static async Task<(NetworkStream? stream, Exception? exception)> AttemptConnection(
			IPAddress address, int port, CancellationToken token, CancellationToken delayToken
		)
		{
			try
			{
				await Task.Delay(-1, delayToken).ConfigureAwait(false);
			}
			catch (TaskCanceledException) { }

			token.ThrowIfCancellationRequested();

			var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };

			try
			{
				await socket.ConnectAsync(address, port, token).ConfigureAwait(false);
				return (new NetworkStream(socket, true), null);
			}
			catch (Exception e)
			{
				socket.Dispose();
				return (null, e);
			}
		}

		private static async Task<List<IPAddress>> GetSortedAddresses(string hostname, CancellationToken token)
		{
			// This method abuses DNS ordering and LINQ a bit. We can normally assume that addresses will be provided in
			// the order the system wants to use. GroupBy will return its groups *in the order they're discovered*. Meaning,
			// the first group created will always be the preferred group, and all other groups are in preference order.
			// This means a straight zipper merge is nice and clean and gives us most -> least preferred, repeating.
			var dnsRecords = await Dns.GetHostAddressesAsync(hostname, AddressFamily.Unspecified, token);

			var groups = dnsRecords
			             .GroupBy(a => a.AddressFamily)
			             .Select(g => g.Select(v => v))
			             .ToArray();

			return ZipperMerge(groups).ToList();
		}

		private static IEnumerable<TSource> ZipperMerge<TSource>(params IEnumerable<TSource>[] sources)
		{
			// Adapted from https://github.com/KazWolfe/Dalamud/blob/767cc49ecb80e29dbdda2fa8329d3c3341c964fe/Dalamud/Utility/Util.cs
			var enumerators = new IEnumerator<TSource>[sources.Length];
			try
			{
				for (var i = 0; i < sources.Length; i++)
				{
					enumerators[i] = sources[i].GetEnumerator();
				}

				var hasNext = new bool[enumerators.Length];

				bool MoveNext()
				{
					var anyHasNext = false;
					for (var i = 0; i < enumerators.Length; i++)
					{
						anyHasNext |= hasNext[i] = enumerators[i].MoveNext();
					}

					return anyHasNext;
				}

				while (MoveNext())
				{
					for (var i = 0; i < enumerators.Length; i++)
					{
						if (hasNext[i])
						{
							yield return enumerators[i].Current;
						}
					}
				}
			}
			finally
			{
				foreach (var enumerator in enumerators)
				{
					enumerator.Dispose();
				}
			}
		}
	}

	private class RedirectHandler : DelegatingHandler
	{
		private int  MaxAutomaticRedirections { get; set; }
		private bool InitialAutoRedirect      { get; set; }

		public RedirectHandler(HttpMessageHandler innerHandler) : base(innerHandler)
		{
			var mostInnerHandler = innerHandler.GetMostInnerHandler();
			SetupCustomAutoRedirect(mostInnerHandler);
		}

		private void SetupCustomAutoRedirect(HttpMessageHandler? mostInnerHandler)
		{
			//Store the initial auto-redirect & max-auto-redirect values.
			//Disabling auto-redirect and handle redirects manually.
			try
			{
				switch (mostInnerHandler)
				{
					case HttpClientHandler hch:
						InitialAutoRedirect      = hch.AllowAutoRedirect;
						MaxAutomaticRedirections = hch.MaxAutomaticRedirections;
						hch.AllowAutoRedirect    = false;
						break;
					case SocketsHttpHandler shh:
						InitialAutoRedirect      = shh.AllowAutoRedirect;
						MaxAutomaticRedirections = shh.MaxAutomaticRedirections;
						shh.AllowAutoRedirect    = false;
						break;
					default:
						Debug.WriteLine("[SetupCustomAutoRedirect] Unknown handler type: {0}",
						                mostInnerHandler?.GetType().FullName);
						InitialAutoRedirect      = true;
						MaxAutomaticRedirections = 17;
						break;
				}
			}
			catch (Exception e)
			{
				Debug.WriteLine(e.Message);
				InitialAutoRedirect      = true;
				MaxAutomaticRedirections = 17;
			}
		}

		private bool IsRedirectAllowed(HttpRequestMessage request)
		{
			var value = request.GetAutoRedirect();
			if (value == null)
				return InitialAutoRedirect;

			return value == true;
		}

		protected override async Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request, CancellationToken cancellationToken
		)
		{
			var redirectCount = 0;
			var response      = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

			//Manual Redirect
			//https://github.com/dotnet/runtime/blob/ccfe21882e4a2206ce49cd5b32d3eb3cab3e530f/src/libraries/System.Net.Http/src/System/Net/Http/SocketsHttpHandler/RedirectHandler.cs
			Uri? redirectUri;
			while (IsRedirect(response) &&
			       IsRedirectAllowed(request) &&
			       (redirectUri = GetUriForRedirect(request.RequestUri!, response)) != null)
			{
				redirectCount++;
				if (redirectCount > MaxAutomaticRedirections)
					break;

				response.Dispose();

				// Clear the authorization header.
				request.Headers.Authorization = null;
				// Set up for the redirect
				request.RequestUri = redirectUri;

				if (RequestRequiresForceGet(response.StatusCode, request.Method))
				{
					request.Method  = HttpMethod.Get;
					request.Content = null;
					if (request.Headers.TransferEncodingChunked == true)
						request.Headers.TransferEncodingChunked = false;
				}

				// Issue the redirected request.
				response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
			}

			return response;
		}

		private static bool IsRedirect(HttpResponseMessage response)
		{
			switch (response.StatusCode)
			{
				case HttpStatusCode.MultipleChoices:
				case HttpStatusCode.Moved:
				case HttpStatusCode.Found:
				case HttpStatusCode.SeeOther:
				case HttpStatusCode.TemporaryRedirect:
				case HttpStatusCode.PermanentRedirect:
					return true;

				default:
					return false;
			}
		}

		private static Uri? GetUriForRedirect(Uri requestUri, HttpResponseMessage response)
		{
			var location = response.Headers.Location;
			if (location == null)
			{
				return null;
			}

			// Ensure the redirect location is an absolute URI.
			if (!location.IsAbsoluteUri)
			{
				location = new Uri(requestUri, location);
			}

			// Per https://tools.ietf.org/html/rfc7231#section-7.1.2, a redirect location without a
			// fragment should inherit the fragment from the original URI.
			var requestFragment = requestUri.Fragment;
			if (!string.IsNullOrEmpty(requestFragment))
			{
				var redirectFragment = location.Fragment;
				if (string.IsNullOrEmpty(redirectFragment))
				{
					location = new UriBuilder(location) { Fragment = requestFragment }.Uri;
				}
			}

			// Reject circular redirects
			return location == requestUri ? null : location;
		}

		private static bool RequestRequiresForceGet(HttpStatusCode statusCode, HttpMethod requestMethod)
		{
			switch (statusCode)
			{
				case HttpStatusCode.Moved:
				case HttpStatusCode.Found:
				case HttpStatusCode.MultipleChoices:
					return requestMethod == HttpMethod.Post;
				case HttpStatusCode.SeeOther:
					return requestMethod != HttpMethod.Get && requestMethod != HttpMethod.Head;
				default:
					return false;
			}
		}
	}
}
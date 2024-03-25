using System.Net;
using System.Net.Sockets;
using Iceshrimp.Backend.Core.Configuration;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Services;

public class CustomHttpClient : HttpClient
{
	private static readonly HttpMessageHandler Handler = new SocketsHttpHandler
	{
		AutomaticDecompression      = DecompressionMethods.All,
		ConnectCallback             = new FastFallback().ConnectCallback,
		PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
		PooledConnectionLifetime    = TimeSpan.FromMinutes(60)
	};

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
}
using System.Buffers;

namespace Iceshrimp.Backend.Core.Extensions;

public static class StreamExtensions
{
	public static async Task CopyToAsync(
		this Stream source, Stream destination, long? maxLength, CancellationToken cancellationToken
	)
	{
		var buffer = ArrayPool<byte>.Shared.Rent(81920);
		try
		{
			int bytesRead;
			var totalBytesRead = 0L;
			while ((maxLength == null || totalBytesRead <= maxLength) && (bytesRead = await DoRead()) != 0)
			{
				totalBytesRead += bytesRead;
				await destination.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, bytesRead), cancellationToken);
			}
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(buffer);
		}

		return;

		ValueTask<int> DoRead() => source.ReadAsync(new Memory<byte>(buffer), cancellationToken);
	}
}
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
			while ((maxLength == null || totalBytesRead <= maxLength) && (bytesRead = await DoReadAsync()) != 0)
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

		ValueTask<int> DoReadAsync() => source.ReadAsync(new Memory<byte>(buffer), cancellationToken);
	}
	
	/// <summary>
	///     We can't trust the Content-Length header, and it might be null.
	///     This makes sure that we only ever read up to maxLength into memory.
	/// </summary>
	/// <param name="stream">The response content stream</param>
	/// <param name="maxLength">The maximum length to buffer (null = unlimited)</param>
	/// <param name="contentLength">The content length, if known</param>
	/// <param name="token">A CancellationToken, if applicable</param>
	/// <returns>Either a buffered MemoryStream, or Stream.Null</returns>
	public static async Task<Stream> GetSafeStreamOrNullAsync(
		this Stream stream, long? maxLength, long? contentLength, CancellationToken token = default
	)
	{
		if (maxLength is 0) return Stream.Null;
		if (contentLength > maxLength) return Stream.Null;

		MemoryStream buf = new();
		if (contentLength < maxLength)
			maxLength = contentLength.Value;

		await stream.CopyToAsync(buf, maxLength, token);
		if (maxLength == null || buf.Length <= maxLength)
		{
			buf.Seek(0, SeekOrigin.Begin);
			return buf;
		}

		await buf.DisposeAsync();
		return Stream.Null;
	}
}
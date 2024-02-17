using ProtoBuf;

namespace Iceshrimp.Backend.Core.Helpers;

public static class RedisHelpers
{
	public static byte[] Serialize<T>(T? data)
	{
		using var stream = new MemoryStream();
		//TODO: use ProtoBuf.Serializer.PrepareSerializer<>();
		Serializer.Serialize(stream, data);
		return stream.ToArray();
	}

	public static T? Deserialize<T>(byte[] buffer)
	{
		using var stream = new MemoryStream(buffer);
		try
		{
			return Serializer.Deserialize<T?>(stream);
		}
		catch
		{
			return default;
		}
	}
}
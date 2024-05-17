using System.Net;
using System.Net.Sockets;

namespace Iceshrimp.Backend.Core.Extensions;

public static class IPAddressExtensions
{
	public static bool IsLoopback(this IPAddress address) => IPAddress.IsLoopback(address);

	public static bool IsLocalIPv6(this IPAddress address) => address.AddressFamily == AddressFamily.InterNetworkV6 &&
	                                                          (address.IsIPv6LinkLocal ||
	                                                           address.IsIPv6SiteLocal ||
	                                                           address.IsIPv6UniqueLocal);

	public static bool IsLocalIPv4(this IPAddress address) => address.AddressFamily == AddressFamily.InterNetwork &&
	                                                          IsPrivateIPv4(address.GetAddressBytes());

	private static bool IsPrivateIPv4(byte[] ipv4Bytes)
	{
		return IsLinkLocal() || IsClassA() || IsClassC() || IsClassB();

		// Link local (no IP assigned by DHCP): 169.254.0.0 to 169.254.255.255 (169.254.0.0/16)
		bool IsLinkLocal() => ipv4Bytes[0] == 169 && ipv4Bytes[1] == 254;

		// Class A private range: 10.0.0.0 – 10.255.255.255 (10.0.0.0/8)
		bool IsClassA() => ipv4Bytes[0] == 10;

		// Class B private range: 172.16.0.0 – 172.31.255.255 (172.16.0.0/12)
		bool IsClassB() => ipv4Bytes[0] == 172 && ipv4Bytes[1] >= 16 && ipv4Bytes[1] <= 31;

		// Class C private range: 192.168.0.0 – 192.168.255.255 (192.168.0.0/16)
		bool IsClassC() => ipv4Bytes[0] == 192 && ipv4Bytes[1] == 168;
	}
}
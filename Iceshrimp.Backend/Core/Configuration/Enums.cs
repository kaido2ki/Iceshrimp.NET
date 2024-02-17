namespace Iceshrimp.Backend.Core.Configuration;

public static class Enums
{
	public enum FederationMode
	{
		BlockList = 0,
		AllowList = 1
	}

	public enum FileStorage
	{
		Local         = 0,
		ObjectStorage = 1
	}

	public enum ItemVisibility
	{
		Hide       = 0,
		Registered = 1,
		Public     = 2
	}

	public enum Registrations
	{
		Closed = 0,
		Invite = 1,
		Open   = 2
	}
}
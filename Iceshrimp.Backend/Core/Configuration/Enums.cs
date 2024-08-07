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

	public enum ImageProcessor
	{
		None       = 0,
		ImageSharp = 1,
		LibVips    = 2
	}

	public enum ItemVisibility
	{
		Hide       = 0,
		Registered = 1,
		Public     = 2
	}

	public enum PublicPreview
	{
		Lockdown          = 0,
		RestrictedNoMedia = 1,
		Restricted        = 2,
		Public            = 3
	}

	public enum Registrations
	{
		Closed = 0,
		Invite = 1,
		Open   = 2
	}
}
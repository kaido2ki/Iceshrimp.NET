namespace Iceshrimp.Shared.Schemas.Web;

public class RelaySchemas
{
	public enum RelayStatus
	{
		Requesting = 0,
		Accepted = 1,
		Rejected = 2
	}

	public class RelayResponse
	{
		public required string      Id     { get; set; }
		public required string      Inbox  { get; set; }
		public required RelayStatus Status { get; set; }
	}

	public class RelayRequest
	{
		public required string Inbox { get; set; }
	}
}
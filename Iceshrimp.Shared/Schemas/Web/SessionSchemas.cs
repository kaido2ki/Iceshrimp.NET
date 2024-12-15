using Iceshrimp.Shared.Helpers;

namespace Iceshrimp.Shared.Schemas.Web;

public class SessionSchemas
{
	public class SessionResponse : IIdentifiable
	{
		public required string    Id         { get; set; }
		public required bool      Current    { get; set; }
		public required bool      Active     { get; set; }
		public required DateTime  CreatedAt  { get; set; }
		public required DateTime? LastActive { get; set; }
	}
}

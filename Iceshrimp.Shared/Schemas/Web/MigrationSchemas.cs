namespace Iceshrimp.Shared.Schemas.Web;

public class MigrationSchemas
{
	public class MigrationRequest
	{
		public string? UserId { get; set; }
		public string? UserUri { get; set; }
	}

	public class MigrationStatusResponse
	{
		public required List<string> Aliases { get; set; }
		public required string?      MovedTo { get; set; }
	}
}
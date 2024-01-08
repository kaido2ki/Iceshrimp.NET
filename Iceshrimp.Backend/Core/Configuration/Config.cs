namespace Iceshrimp.Backend.Core.Configuration;

public sealed class Config {
	// FIXME: This doesn't reflect config updates.
	public static   Config          StartupConfig { get; set; } = null!;
	
	public required InstanceSection Instance      { get; set; }
	public required DatabaseSection Database      { get; set; }

	public sealed class InstanceSection {
		public required int    ListenPort    { get; set; } = 3000;
		public required string WebDomain     { get; set; }
		public required string AccountDomain { get; set; }
	}

	public sealed class DatabaseSection {
		public required string  Host     { get; set; }
		public required string  Database { get; set; }
		public required string  Username { get; set; }
		public          string? Password { get; set; }
	}
}
namespace Iceshrimp.Backend.Core.Configuration;

// TODO: something something IConfiguration
public class Config {
	public static Config Instance = new() {
		Url = "https://shrimp-next.fedi.solutions"
	};

	public required string Url { get; set; }
}
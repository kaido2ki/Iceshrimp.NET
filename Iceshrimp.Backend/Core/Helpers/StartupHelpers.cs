using System.Reflection;

namespace Iceshrimp.Backend.Core.Helpers;

public static class StartupHelpers
{
	public static void ParseCliArguments(string[] args)
	{
		if (args.Contains("-h") || args.Contains("--help") || args.Contains("-?"))
		{
			Console.WriteLine($"""
			                   Usage: ./{typeof(Program).Assembly.GetName().Name} [options...]
			                    -h, -?, --help        Prints information on available command line arguments.
			                    --migrate             Applies pending migrations.
			                    --migrate-and-start   Applies pending migrations, then starts the application.
			                    --printconfig         Prints the example config.
			                    --recompute-counters  Recomputes denormalized database counters.
			                    --migrate-storage     Migrates all files currently stored locally to the
			                                          configured object storage bucket.
			                    --https               For development purposes only. Listens using https
			                                          instead of http on the specified port.
			                    --environment <env>   Specifies the ASP.NET Core environment. Available options
			                                          are 'Development' and 'Production'.
			                   """);
			Environment.Exit(0);
		}

		if (args.Contains("--printconfig"))
		{
			var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			var config      = File.ReadAllText(Path.Join(assemblyDir, "configuration.ini"));
			Console.WriteLine(config);
			Environment.Exit(0);
		}
	}
}